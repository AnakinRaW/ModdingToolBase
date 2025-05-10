using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal class BackupManager : IBackupManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFileSystem _fileSystem;
    private readonly ConcurrentDictionary<IInstallableComponent, BackupValueData> _backups = new(ProductComponentIdentityComparer.Default);
    private readonly ILogger? _logger;
    private readonly IProductService _productService;
    private readonly IHashingService _hashingService;

    //[field: AllowNull]
    private IFileRepository FileRepository => LazyInitializer.EnsureInitialized(ref field, CreateRepository);

    public IDictionary<IInstallableComponent, BackupValueData> Backups => new Dictionary<IInstallableComponent, BackupValueData>(_backups);

    public BackupManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = _serviceProvider.GetRequiredService<IFileSystem>();
        _productService = serviceProvider.GetRequiredService<IProductService>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
    }

    private FileRepository CreateRepository()
    {
        var config = _serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();

        if (config.BackupPolicy == BackupPolicy.Disable)
            throw new NotSupportedException("Backup manager should not be called when with disabled backed policy.");

        if (string.IsNullOrEmpty(config.BackupLocation))
            throw new InvalidOperationException("Backup location not specified");

        return new FileRepository(config.BackupLocation!, _fileSystem, "bak");
    }

    public void BackupComponent(IInstallableComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));

        var backupData = _backups.GetOrAdd(component, CreateBackupEntry);

        // Check whether the component is actually present
        if (backupData.IsOriginallyMissing())
            return;

        try
        {
            var backup = backupData.Backup;
            backup!.Directory!.Create();
            backupData.Destination.CopyWithRetry(backup.FullName);
        }
        catch (Exception)
        {
            RemoveBackup(component);
            throw;
        }
    }
    
    public void RestoreBackup(IInstallableComponent component)
    {
        if (component == null) 
            throw new ArgumentNullException(nameof(component));

        if (!_backups.TryRemove(component, out var backupData))
            return;

        var destination = backupData.Destination;

        destination.Refresh();

        if (backupData.IsOriginallyMissing())
        {
            if (!destination.Exists)
                return;
            if (destination.TryDeleteWithRetry())
                return;
            throw new IOException("Unable to restore the backup. Please restart your computer!");
        }

        var backup = backupData.Backup;
        backup!.Refresh();
        if (!backup.Exists)
            throw new FileNotFoundException("Source file not found", backup.FullName);

        try
        {
            if (destination.Exists)
            {
                var backHash = _hashingService.GetHash(backup, HashTypeKey.SHA256);
                var sourceHash = _hashingService.GetHash(destination, HashTypeKey.SHA256);
                if (backHash.SequenceEqual(sourceHash))
                    return;
            }

            backupData.Backup!.CopyWithRetry(backupData.Destination.FullName);
        }
        finally
        {
            FileRepository.RemoveComponent(component);
        }
    }

    public void RemoveBackup(IInstallableComponent component)
    {
        _backups.TryRemove(component, out _);
        FileRepository.RemoveComponent(component);
    }

    public void RestoreAll()
    {
        foreach (var component in _backups.Keys) 
            RestoreBackup(component);
    }

    private BackupValueData CreateBackupEntry(IInstallableComponent component)
    {
        if (component is not SingleFileComponent singleFileComponent)
            throw new NotSupportedException($"option '{nameof(component)}' must be of type '{nameof(SingleFileComponent)}'");

        var variables = _productService.GetCurrentInstance().Variables;
        var destination = singleFileComponent.GetFile(_fileSystem, variables);

        if (component.DetectedState == DetectionState.Absent)
            return new BackupValueData(destination);

        if (!destination.Exists)
        {
            var e = new FileNotFoundException("Could not find source file to backup.");
            _logger?.LogError(e, e.Message);
            throw e;
        }

        return new BackupValueData(destination)
        {
            Backup = FileRepository.AddComponent(component, variables)
        };
    }
}