using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Updater.Tasks;

internal class DownloadStep(
    InstallableComponent installable,
    UpdateConfiguration updateConfiguration,
    IDownloadManager downloadManager,
    IReadOnlyDictionary<string, string> productVariables,
    IServiceProvider serviceProvider)
    : SynchronizedStep(serviceProvider), IComponentStep
{
    private readonly UpdateConfiguration _updateConfiguration = updateConfiguration ?? throw new ArgumentNullException(nameof(updateConfiguration));
    private readonly IDownloadRepositoryFactory _downloadRepositoryFactory = serviceProvider.GetRequiredService<IDownloadRepositoryFactory>();

    public event EventHandler<ProgressEventArgs<ComponentProgressInfo>>? Progress;

    public ProgressType Type => ProgressTypes.Download;

    public IFileInfo DownloadPath { get; private set; } = null!;

    public long Size { get; } = installable.DownloadSize;

    private Uri Uri { get; } = installable.OriginInfo!.Url;

    private InstallableComponent Component { get; } = installable ?? throw new ArgumentNullException(nameof(installable));

    ProductComponent IComponentStep.Component => Component;

    public override string ToString()
    {
        return $"Downloading component '{Component.GetUniqueId()}' form \"{Uri}\"";
    }

    protected override void RunSynchronized(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        try
        {
            ReportProgress(0.0);
            
            Exception? lastException = null;
            if (!token.IsCancellationRequested)
                DownloadAction(token, out lastException);

            token.ThrowIfCancellationRequested();

            if (lastException != null)
            {
                var action = lastException is DownloadValidationFailedException ? "validate download" : "download";
                Logger?.LogError(lastException, $"Failed to {action} from '{Uri}'. {lastException.Message}");
                throw lastException;
            }
        }
        finally
        {
            ReportProgress(1.0);
        }
    }

    private void ReportProgress(double progress)
    {
        Progress?.Invoke(this, new ProgressEventArgs<ComponentProgressInfo>(progress, "_"));
    }

    private void DownloadAction(CancellationToken token, out Exception? lastException)
    {
        lastException = null;

        try
        {
            var downloadPath = _downloadRepositoryFactory.GetRepository().AddComponent(Component, productVariables);
            DownloadPath = downloadPath;

            var downloadRetryCount = 1 + _updateConfiguration.DownloadRetryCount;

            for (var i = 0; i < downloadRetryCount; i++)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    DownloadAndVerifyAsync(DownloadPath, token).Wait(CancellationToken.None);
                    DownloadPath.Refresh();
                    if (!DownloadPath.Exists)
                    {
                        var message = "Source not found after being successfully downloaded and verified: " +
                                      DownloadPath + ", package: " + Component.GetDisplayName();
                        Logger?.LogWarning(message);
                        throw new FileNotFoundException(message, DownloadPath.FullName);
                    }

                    lastException = null;

                    break;
                }
                catch (OperationCanceledException ex)
                {
                    lastException = ex;
                    Logger?.LogWarning($"Download of '{Uri}' was cancelled.");
                    break;
                }
                catch (Exception e) when (e.IsExceptionType<UnauthorizedAccessException>())
                {
                    ExceptionDispatchInfo.Capture(e.InnerException!).Throw();
                }
                catch (Exception ex)
                {
                    if (ex is AggregateException && ex.IsExceptionType<OperationCanceledException>())
                    {
                        lastException = ex;
                        Logger?.LogWarning($"Download of {Uri} was cancelled.");
                        break;
                    }
                    var wrappedException = ex.TryGetWrappedException();
                    if (wrappedException != null)
                        ex = wrappedException;
                    lastException = ex;
                    Logger?.LogError(ex, $"Failed to download \"{Uri}\" on try {i}: {ex.Message}");
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            lastException = ex;
            Logger?.LogError(ex, $"Failed to create download path '{DownloadPath}' due to missing permission: {ex.Message}");
            var restartManager = Services.GetRequiredService<IRestartManager>();
            restartManager.SetRestart(RestartType.ApplicationElevation);
        }
    }

    private async Task DownloadAndVerifyAsync(IFileInfo destination, CancellationToken token)
    {
        var integrityInformation = Component.OriginInfo!.IntegrityInformation;
        try
        {
#if NETSTANDARD2_1
            await using var file = destination.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
#else
            using var file = destination.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
#endif
            await downloadManager.DownloadAsync(Uri, file, OnProgress, null,
                new HashDownloadValidator(integrityInformation.Hash, integrityInformation.HashType, Services), token);

        }
        catch (OperationCanceledException)
        {
            try
            {
                Logger?.LogTrace(
                    $"Deleting potentially partially downloaded file '{destination}' generated as a result of operation cancellation.");
                destination.Delete();
            }
            catch (Exception e)
            {
                Logger?.LogTrace($"Could not delete partially downloaded file '{destination}' due to exception: {e}");
            }

            throw;
        }
    }

    private void OnProgress(DownloadUpdate status)
    {
        var progress = (double)status.BytesRead / Size;
        ReportProgress(progress);
    }
}