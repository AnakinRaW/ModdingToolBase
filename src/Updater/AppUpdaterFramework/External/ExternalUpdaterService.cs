using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.CommonUtilities;
using AnakinRaW.ExternalUpdater;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.External;

internal class ExternalUpdaterService : IExternalUpdaterService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly IProductService _productService;
    private readonly IExternalUpdaterLauncher _launcher;
    private readonly IPendingComponentStore _pendingComponentStore;
    private readonly IReadonlyBackupManager _backupManager;
    private readonly IReadonlyDownloadRepository _downloadRepository;
    private readonly ICurrentProcessInfoProvider _currentProcessInfoProvider;

    private readonly string _tempPath;

    public ExternalUpdaterService(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
        _serviceProvider = serviceProvider;
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _productService = serviceProvider.GetRequiredService<IProductService>();
        _launcher = serviceProvider.GetRequiredService<IExternalUpdaterLauncher>();
        _pendingComponentStore = serviceProvider.GetRequiredService<IPendingComponentStore>();
        _backupManager = serviceProvider.GetRequiredService<IReadonlyBackupManager>();
        _downloadRepository = serviceProvider.GetRequiredService<IReadonlyDownloadRepository>();
        _currentProcessInfoProvider = serviceProvider.GetRequiredService<ICurrentProcessInfoProvider>();

        _tempPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.GetTempPath());
    }

    public UpdateOptions CreateUpdateOptions()
    {
        var cpi = _currentProcessInfoProvider.GetCurrentProcessInfo();
        if (string.IsNullOrEmpty(cpi.ProcessFilePath))
            throw new InvalidOperationException("The current process is not running from a file");
        var updateInformationFile = WriteToTempFile(CollectUpdateInformation());

        return new UpdateOptions
        {
            AppToStart = cpi.ProcessFilePath,
            Pid = cpi.Id,
            UpdateFile = updateInformationFile,
            LoggingDirectory = _tempPath
        };
    }

    public RestartOptions CreateRestartOptions(bool elevate)
    {
        var cpi = _currentProcessInfoProvider.GetCurrentProcessInfo();
        if (string.IsNullOrEmpty(cpi.ProcessFilePath))
            throw new InvalidOperationException("The current process is not running from a file");
        return new RestartOptions
        {
            AppToStart = cpi.ProcessFilePath,
            Pid = cpi.Id,
            Elevate = elevate,
            LoggingDirectory = _tempPath
        };
    }

    public IFileInfo GetExternalUpdater()
    {
        var updater = _productService.GetInstalledComponents().Items
            .FirstOrDefault(c => c.Id == ExternalUpdaterConstants.ComponentIdentity);

        if (updater is not SingleFileComponent updaterComponent)
            throw new NotSupportedException("External updater component not registered to current product.");

        var filePath = updaterComponent.GetFullPath(_serviceProvider, _productService.GetCurrentInstance().Variables);
        return _fileSystem.FileInfo.New(filePath);
    }

    public void Launch(ExternalUpdaterOptions options)
    {
        var updater = GetExternalUpdater();
        _launcher.Start(updater, options);
    }

    private IEnumerable<UpdateInformation> CollectUpdateInformation()
    {
        var pendingComponents = _pendingComponentStore.PendingComponents;
        var backups = _backupManager.Backups;

        var updateInformation = new List<UpdateInformation>();

        foreach (var pendingComponent in pendingComponents)
        {
            if (pendingComponent.Action == UpdateAction.Keep)
                continue;
            if (pendingComponent.Component is not IPhysicalInstallable physicalInstallable)
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
        var tempPath = _fileSystem.Path.GetTempPath();
        var fileName = _fileSystem.Path.GetTempFileName();
        var tempFilePath = _fileSystem.Path.Combine(tempPath, fileName);

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

    private FileCopyInformation CreateFromComponent(IPhysicalInstallable component, UpdateAction action)
    {
        if (action == UpdateAction.Keep)
            throw new NotSupportedException("UpdateAction Keep is not supported");

        var componentLocation = component.GetFullPath(_serviceProvider, _productService.GetCurrentInstance().Variables);

        string? destination;
        string source;

        switch (action)
        {
            case UpdateAction.Update:
                source = _downloadRepository.GetComponent(component)?.FullName ??
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
}