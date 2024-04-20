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
    private readonly List<IInstallableComponent> _filesFailedToBeCleaned = new();
    private readonly IBackupManager _backupManager = serviceProvider.GetRequiredService<IBackupManager>();
    private readonly IDownloadRepository _downloadRepository = serviceProvider.GetRequiredService<IDownloadRepository>();
    private readonly List<IInstallableComponent> _downloadsToClean = new();
    private readonly List<IInstallableComponent> _backupsToClean = new();

    protected override Task<bool> PrepareCoreAsync()
    {
        _backupsToClean.Clear();
        _downloadsToClean.Clear();

        _backupsToClean.AddRange(_backupManager.Backups.Keys);
        _downloadsToClean.AddRange(_downloadRepository.GetComponents().Keys);
        return Task.FromResult(true);
    }

    protected override Task RunCoreAsync(CancellationToken token)
    {
        if (!_downloadsToClean.Any() && !_backupsToClean.Any())
        {
            Logger?.LogTrace("No files to clean up");
            return Task.CompletedTask;
        }

        _filesFailedToBeCleaned.Clear();
        
        foreach (var backup in _backupsToClean) 
            GuardedClean(backup, _backupManager.RemoveBackup);

        foreach (var download in _downloadsToClean)
            GuardedClean(download, _downloadRepository.RemoveComponent);


        if (_filesFailedToBeCleaned.Any())
        {
            Logger?.LogTrace("These components could not be deleted:");
            foreach (var file in _filesFailedToBeCleaned)
                Logger?.LogTrace(file.GetDisplayName());
        }

        return Task.CompletedTask;
    }

    private void GuardedClean(IInstallableComponent component,  Action<IInstallableComponent> cleanOperation)
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