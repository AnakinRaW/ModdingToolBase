using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.ExternalUpdater;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using IServiceProvider = System.IServiceProvider;

namespace AnakinRaW.AppUpdaterFramework.External;

internal class ExternalUpdaterService : IExternalUpdaterService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly IProductService _productService;
    private readonly IPendingComponentStore _pendingComponentStore;
    private readonly IReadOnlyBackupManager _backupManager;
    private readonly IReadOnlyFileRepository _downloadFileRepository;
    private readonly UpdateConfiguration _updateConfig;
    private readonly IExternalUpdaterProvider _updaterProvider;
    private readonly IExternalUpdaterIntegrityCheck _integrityCheck;
    private readonly string _tempPath;

    public ExternalUpdaterService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _productService = serviceProvider.GetRequiredService<IProductService>();
        _pendingComponentStore = serviceProvider.GetRequiredService<IPendingComponentStore>();
        _backupManager = serviceProvider.GetRequiredService<IReadOnlyBackupManager>();
        _downloadFileRepository = serviceProvider.GetRequiredService<IDownloadRepositoryFactory>().GetReadOnlyRepository();
        _updateConfig = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
        serviceProvider.GetRequiredService<IHashingService>();
        _updaterProvider = serviceProvider.GetRequiredService<IExternalUpdaterProvider>();
        _integrityCheck = serviceProvider.GetRequiredService<IExternalUpdaterIntegrityCheck>();
        serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(ExternalUpdaterService));

        // Must be trimmed as otherwise paths enclosed in quotes and a trailing separator cause commandline arg parsing errors
        _tempPath = PathNormalizer.Normalize(_fileSystem.Path.GetTempPath(), PathNormalizeOptions.TrimTrailingSeparators);
    }

    public ExternalUpdateOptions CreateUpdateOptions()
    {
        var cpi = CurrentProcessInfo.Current;
        if (string.IsNullOrEmpty(cpi.ProcessFilePath))
            throw new InvalidOperationException("The current process is not running from a file");
        var updateInformationFile = WriteToTempFile(CollectUpdateInformation());

        return new ExternalUpdateOptions
        {
            AppToStart = cpi.ProcessFilePath!,
            AppToStartArguments = CreateAppStartArguments(),
            Pid = cpi.Id,
            UpdateFile = updateInformationFile,
            LoggingDirectory = _tempPath,
#if DEBUG
            Timeout = 90,
#endif
        };
    }

    public ExternalRestartOptions CreateRestartOptions(bool elevate)
    {
        var cpi = CurrentProcessInfo.Current;
        if (string.IsNullOrEmpty(cpi.ProcessFilePath))
            throw new InvalidOperationException("The current process is not running from a file");

        return new ExternalRestartOptions
        {
            AppToStart = cpi.ProcessFilePath!,
            AppToStartArguments = CreateAppStartArguments(),
            Pid = cpi.Id,
            Elevate = elevate,
            LoggingDirectory = _tempPath,
#if DEBUG
            Timeout = 90,
#endif
        };
    }

    public IFileInfo GetExternalUpdater()
    {
        return ResolveExternalUpdaterFile();
    }

    public void Launch(ExternalUpdaterOptions options)
    {
        var file = ResolveExternalUpdaterFile();
        var integrityInformation = GetTrustedIntegrityInformation();
        _integrityCheck.EnsureMatchesAny(file, integrityInformation);

        var launcher = _serviceProvider.GetRequiredService<IExternalUpdaterLauncher>();
        using var _ = launcher.Start(file, options);
    }

    private IFileInfo ResolveExternalUpdaterFile()
    {
        var updater = ResolveExternalUpdaterComponent();
        var filePath = updater.GetFullPath(_fileSystem, _productService.GetCurrentInstance().Variables);
        return _fileSystem.FileInfo.New(filePath);
    }

    private SingleFileComponent ResolveExternalUpdaterComponent()
    {
        var updater = _productService.GetCurrentInstance().Manifest.Components
            .OfType<SingleFileComponent>()
            .FirstOrDefault(c => c.Id == ExternalUpdaterConstants.ComponentIdentity);
        return updater ?? throw new InvalidOperationException("External updater component not registered to current product.");
    }

    private IReadOnlyCollection<ComponentIntegrityInformation> GetTrustedIntegrityInformation()
    {
        var acceptable = new List<ComponentIntegrityInformation>();

        var trusted = _updaterProvider.GetIntegrity();
        if (trusted.HashType == HashTypeKey.None || trusted.Hash is null)
            throw new InvalidOperationException("Unable to get integrity information for external updater.");
        
        acceptable.Add(trusted);

        var pending = _pendingComponentStore.PendingComponents
            .Select(p => p.Component)
            .OfType<SingleFileComponent>()
            .FirstOrDefault(c => c.Id == ExternalUpdaterConstants.ComponentIdentity);
        
        if (pending is not null)
        {
            var pendingIntegrity = pending.OriginInfo?.IntegrityInformation ?? ComponentIntegrityInformation.None;
            var signingRequired = _updateConfig.ManifestSigningConfiguration.Policy == SignaturePolicy.Required;
            
            if (signingRequired && (pendingIntegrity.HashType == HashTypeKey.None || pendingIntegrity.Hash is null))
                throw new InvalidOperationException(
                    "Update security policy requires signing, but the pending external updater component has no integrity declaration. The signed-manifest pipeline is expected to populate it.");
            
            acceptable.Add(pendingIntegrity);
        }

        return acceptable;
    }

    private List<UpdateInformation> CollectUpdateInformation()
    {
        var pendingComponents = _pendingComponentStore.PendingComponents;
        var backups = _backupManager.Backups;

        var updateInformation = new List<UpdateInformation>();

        foreach (var pendingComponent in pendingComponents)
        {
            if (pendingComponent.Action == UpdateAction.Keep)
                continue;
            if (pendingComponent.Component is not PhysicallyInstallableComponent physicalInstallable)
                throw new NotSupportedException("Non physical components are currently not supported");

            BackupInformation? backupInformation = null;
            if (backups.TryGetValue(pendingComponent.Component, out var backup))
            {
                backupInformation = CreateFromBackup(backup);
                backups.Remove(pendingComponent.Component);
            }

            var copyInformation = CreateFromComponent(physicalInstallable, pendingComponent.Action);

            var item = new UpdateInformation
            {
                Update = copyInformation,
                Backup = backupInformation
            };

            updateInformation.Add(item);
        }

        foreach (var backup in backups.Values)
        {
            var backupInformation = CreateFromBackup(backup);
            var item = new UpdateInformation
            {
                Backup = backupInformation
            };
            updateInformation.Add(item);
        }

        return updateInformation;
    }

    private string WriteToTempFile(IEnumerable<UpdateInformation> updateInformation)
    {
        var fileName = _fileSystem.Path.GetRandomFileName();
        var tempFilePath = _fileSystem.Path.Combine(_tempPath, fileName);

        using var fs = _fileSystem.FileStream.New(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        using var writer = new StreamWriter(fs);
        writer.Write(updateInformation.Serialize());

        return tempFilePath;
    }

    private static BackupInformation CreateFromBackup(BackupValueData backup)
    {
        return new BackupInformation
        {
            Destination = backup.Destination.FullName,
            Source = backup.Backup?.FullName
        };
    }

    private FileCopyInformation CreateFromComponent(PhysicallyInstallableComponent component, UpdateAction action)
    {
        if (action == UpdateAction.Keep)
            throw new NotSupportedException("UpdateAction Keep is not supported");

        var componentLocation = component.GetFullPath(_fileSystem, _productService.GetCurrentInstance().Variables);

        string? destination;
        string source;

        switch (action)
        {
            case UpdateAction.Update:
                source = _downloadFileRepository.GetComponent(component)?.FullName ??
                         throw new InvalidOperationException(
                             $"Unable to find source location for component: {component}");
                destination = componentLocation;
                break;
            case UpdateAction.Delete:
                source = componentLocation;
                destination = null;
                break;
            case UpdateAction.Keep:
                throw new NotSupportedException("UpdateAction Keep is not supported");
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
        return new FileCopyInformation
        {
            Destination = destination,
            File = source
        };
    }

    private string? CreateAppStartArguments()
    {
        return _updateConfig.RestartConfiguration.PassCurrentArgumentsForRestart ? 
            ExternalUpdaterArgumentUtilities.GetCurrentApplicationCommandLineForPassThroughAsBase64() : 
            null;
    }
}