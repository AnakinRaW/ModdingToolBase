using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Runners;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal sealed class UpdatePipeline : Pipeline
{
    private readonly IComponentProgressReporter _progressReporter;
    private readonly IInstalledProduct _installedProduct;

    private readonly HashSet<IUpdateItem> _itemsToProcess;
    
    private readonly List<DownloadStep> _componentsToDownload = new();
    private readonly List<InstallStep> _installsOrRemoves = new();

    private readonly ParallelStepRunner _downloadsRunner;
    private readonly SequentialStepRunner _installsRunner;
    private readonly IRestartManager _restartManager;

    private readonly IFileSystem _fileSystem;

    private AggregatedDownloadProgressReporter? _downloadProgress;
    private AggregatedInstallProgressReporter? _installProgress;

    public UpdatePipeline(IUpdateCatalog updateCatalog, IComponentProgressReporter progressReporter, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (updateCatalog == null) 
            throw new ArgumentNullException(nameof(updateCatalog));

        _progressReporter = progressReporter;
        _installedProduct = updateCatalog.InstalledProduct;
        _restartManager = serviceProvider.GetRequiredService<IRestartManager>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

        _itemsToProcess = [..updateCatalog.UpdateItems];

        _installsRunner = new SequentialStepRunner(ServiceProvider);
        _downloadsRunner = new ParallelStepRunner(2, ServiceProvider);

        RegisterEvents();
    }

    private void RegisterEvents()
    {
        _downloadsRunner.Error += OnError;
        _installsRunner.Error += OnError;
        _restartManager.RestartRequired += OnRestartRequired;
    }

    private void UnregisterEvents()
    {
        _downloadsRunner.Error -= OnError;
        _installsRunner.Error -= OnError;
        _restartManager.RestartRequired -= OnRestartRequired;

        foreach (var downloadStep in _componentsToDownload) 
            downloadStep.Canceled -= DownloadCancelled;
    }

    protected override Task<bool> PrepareCoreAsync()
    { 
        if (_itemsToProcess.Count == 0)
        {
            var ex = new InvalidOperationException("No items to update/remove.");
            Logger?.LogError(ex, ex.Message);
            return Task.FromResult(false);
        }

        _componentsToDownload.Clear();
        _installsOrRemoves.Clear();

        var configuration = ServiceProvider.GetService<IUpdateConfigurationProvider>()?.GetConfiguration() ??
                            UpdateConfiguration.Default;

        var downloadManager = new DownloadManager(configuration.DownloadConfiguration, ServiceProvider);

        var installs = new List<IUpdateItem>();
        var removes = new List<IUpdateItem>();

        foreach (var updateItem in _itemsToProcess)
        {
            if (updateItem.Action == UpdateAction.Update)
                installs.Add(updateItem);
            else if (updateItem.Action == UpdateAction.Delete)
                removes.Add(updateItem);
        }

        foreach (var itemToInstall in installs)
        {
            if (itemToInstall.UpdateComponent is IPhysicalInstallable physicalInstallable)
            {
                var path = physicalInstallable.GetFullPath(_fileSystem, _installedProduct.Variables);
                var shallNotRemove = removes.Where(x =>
                {
                    if (x.InstalledComponent is not IPhysicalInstallable installed)
                        return false;
                    var otherPath = installed.GetFullPath(_fileSystem, _installedProduct.Variables);
                    return _fileSystem.Path.AreEqual(path, otherPath);
                });

                foreach (var toRemove in shallNotRemove.ToList()) 
                    removes.Remove(toRemove);
            }
        }

        foreach (var updateItem in installs.Concat(removes))
        {
            var installedComponent = updateItem.InstalledComponent;
            var updateComponent = updateItem.UpdateComponent;
            if (updateItem.Action == UpdateAction.Update && updateComponent != null)
            {
                if (updateComponent.OriginInfo is null)
                    throw new InvalidOperationException($"OriginInfo is missing for '{updateComponent}'");

                var downloadTask = new DownloadStep(updateComponent, configuration, downloadManager, _installedProduct.Variables, ServiceProvider);
                downloadTask.Canceled += DownloadCancelled;
                var installTask = new InstallStep(updateComponent, installedComponent, downloadTask, configuration, _installedProduct.Variables, ServiceProvider);

                _installsOrRemoves.Add(installTask);
                _componentsToDownload.Add(downloadTask);
            }

            if (updateItem.Action == UpdateAction.Delete && installedComponent != null)
            {
                var removeTask = new InstallStep(installedComponent, configuration, _installedProduct.Variables, ServiceProvider);
                _installsOrRemoves.Add(removeTask);
            }
        }

        foreach (var d in _componentsToDownload)
            _downloadsRunner.AddStep(d);
        foreach (var installsOrRemove in _installsOrRemoves)
            _installsRunner.AddStep(installsOrRemove);

        if (_componentsToDownload.Count > 0) 
            _downloadProgress = new AggregatedDownloadProgressReporter(_progressReporter, _componentsToDownload);
        if (_installsOrRemoves.Count > 0)
            _installProgress = new AggregatedInstallProgressReporter(_progressReporter, _installsOrRemoves);

        return Task.FromResult(true);
    }

    private void DownloadCancelled(object sender, EventArgs e)
    {
        Cancel();
    }

    protected override async Task RunCoreAsync(CancellationToken token)
    {
        _progressReporter.Report(0.0, "Starting update...", ProgressTypes.Install, new ComponentProgressInfo());

        var componentsToDownload = _componentsToDownload.ToList();
        var componentsToInstallOrRemove = _installsOrRemoves.ToList();

        if (!componentsToDownload.Any())
            _progressReporter.Report(1.0, "_", ProgressTypes.Download, new ComponentProgressInfo());

        if (!componentsToInstallOrRemove.Any())
            _progressReporter.Report(1.0, "_", ProgressTypes.Install, new ComponentProgressInfo());

        try
        {
            Logger?.LogTrace("Starting update job.");
            var downloadTask = _downloadsRunner.RunAsync(token);
#if DEBUG
            await downloadTask;
#endif
            await _installsRunner.RunAsync(token);
            try
            {
                await downloadTask;
            }
            catch
            {
                // Ignore
            }
        }
        finally
        {
            Logger?.LogTrace("Completed update job.");
        }

        if (_restartManager.RequiredRestartType == RestartType.ApplicationElevation)
            throw new ElevationRequiredException();

        token.ThrowIfCancellationRequested();

        var failedDownloads = componentsToDownload.Where(p =>
            p.Error != null && !p.Error.IsExceptionType<OperationCanceledException>());

        var failedInstalls = componentsToInstallOrRemove
            .Where(installTask => !installTask.Result.IsSuccess()).ToList();

        var failedTasks = failedDownloads.Concat<IComponentStep>(failedInstalls).ToList();
        
        if (failedTasks.Count != 0)
            throw new StepFailureException(failedTasks);
    }

    protected override void DisposeResources()
    {
        base.DisposeResources();
        UnregisterEvents();
        _downloadProgress?.Dispose();
        _installProgress?.Dispose();
        _downloadProgress = null;
        _installProgress = null;
    }

    private void OnRestartRequired(object? sender, EventArgs e)
    {
        if (_restartManager.RequiredRestartType != RestartType.ApplicationElevation)
            return;

        Logger?.LogWarning("Elevation requested. Update gets cancelled");
        Cancel();
        _restartManager.RestartRequired -= OnRestartRequired;
    }
}