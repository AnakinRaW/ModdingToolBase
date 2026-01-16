using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.CommonUtilities.SimplePipeline;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class UpdateCleanPipeline(IServiceProvider serviceProvider) : Pipeline(serviceProvider)
{
    private readonly List<InstallableComponent> _filesFailedToBeCleaned = [];
    private readonly IBackupManager _backupManager = serviceProvider.GetRequiredService<IBackupManager>();
    private readonly List<InstallableComponent> _downloadsToClean = [];
    private readonly List<InstallableComponent> _backupsToClean = [];
    private readonly IFileRepository _downloadFileRepository = serviceProvider.GetRequiredService<IDownloadRepositoryFactory>().GetRepository();
    
    protected override Task PrepareCoreAsync(CancellationToken token)
    {
        _backupsToClean.Clear();
        _downloadsToClean.Clear();

        _backupsToClean.AddRange(_backupManager.Backups.Keys);
        _downloadsToClean.AddRange(_downloadFileRepository.GetComponents().Keys);
        return Task.FromResult(true);
    }

    protected override Task ExecuteAsync(CancellationToken token)
    {
        if (_downloadsToClean.Count == 0 && _backupsToClean.Count == 0)
        {
            Logger?.LogTrace("No files to clean up");
            return Task.CompletedTask;
        }

        _filesFailedToBeCleaned.Clear();
        
        foreach (var backup in _backupsToClean) 
            GuardedClean(backup, _backupManager.RemoveBackup);

        foreach (var download in _downloadsToClean)
            GuardedClean(download, _downloadFileRepository.RemoveComponent);


        if (_filesFailedToBeCleaned.Any())
        {
            Logger?.LogTrace("These components could not be deleted:");
            foreach (var file in _filesFailedToBeCleaned)
                Logger?.LogTrace(file.GetDisplayName());
        }

        return Task.CompletedTask;
    }

    private void GuardedClean(InstallableComponent component,  Action<InstallableComponent> cleanOperation)
    {
        try
        {
            cleanOperation(component);
        }
        catch (Exception)
        {
            _filesFailedToBeCleaned.Add(component);
        }
    }
}