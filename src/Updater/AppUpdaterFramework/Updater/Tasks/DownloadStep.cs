
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
using AnakinRaW.CommonUtilities.Hashing;
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
    : PipelineStep(serviceProvider), IComponentStep
{
    private readonly UpdateConfiguration _updateConfiguration = updateConfiguration ?? throw new ArgumentNullException(nameof(updateConfiguration));
    private readonly IDownloadRepositoryFactory _downloadRepositoryFactory = serviceProvider.GetRequiredService<IDownloadRepositoryFactory>();
    private readonly IFileSystem _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    public event EventHandler<ProgressEventArgs<ComponentProgressInfo>>? Progress;

    public ProgressType Type => ProgressTypes.Download;

    public string DownloadPath { get; private set; } = null!;

    public long Size { get; } = installable.DownloadSize;

    private Uri Uri { get; } = installable.OriginInfo!.Url;

    private InstallableComponent Component { get; } = installable ?? throw new ArgumentNullException(nameof(installable));

    ProductComponent IComponentStep.Component => Component;

    public override string ToString()
    {
        return $"Downloading component '{Component.GetUniqueId()}' form '{Uri}' to '{DownloadPath}'";
    }

    protected override async Task RunCoreAsync(CancellationToken token)
    {
        if (token.IsCancellationRequested)
            return;
        try
        {
            ReportProgress(0.0);
            
            Exception? lastException = null;
            if (!token.IsCancellationRequested)
                lastException = await DownloadActionAsync(token).ConfigureAwait(false);

            token.ThrowIfCancellationRequested();

            if (lastException != null)
            {
                var action = lastException is DownloadValidationFailedException ? "validate download" : "download";
                Logger?.LogError(lastException, "Failed to {Action} from '{Uri}'. {Message}", action, Uri, lastException.Message);
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

    private async Task<Exception?> DownloadActionAsync(CancellationToken token)
    {
        Exception? lastException = null;

        try
        {
            DownloadPath = _downloadRepositoryFactory.GetRepository()
                .AddComponent(Component, productVariables);

            if (TryReuseCachedDownload())
                return null;

            var downloadRetryCount = 1 + _updateConfiguration.DownloadRetryCount;

            for (var i = 0; i < downloadRetryCount; i++)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    await DownloadAndVerifyAsync(DownloadPath, token).ConfigureAwait(false);
                    
                    if (!_fileSystem.File.Exists(DownloadPath))
                    {
                        var message = "Source not found after being successfully downloaded and verified: " +
                                      DownloadPath + ", package: " + Component.GetDisplayName();
                        Logger?.LogWarning(message);
                        throw new FileNotFoundException(message, DownloadPath);

                    }
                    
                    lastException = null;
                    break;
                }
                catch (OperationCanceledException ex)
                {
                    lastException = ex;
                    Logger?.LogWarning("Download of '{Uri}' was cancelled.", Uri);
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
                        Logger?.LogWarning("Download of {Uri} was cancelled.", Uri);
                        break;
                    }
                    var wrappedException = ex.TryGetWrappedException();
                    if (wrappedException != null)
                        ex = wrappedException;
                    lastException = ex;
                    Logger?.LogError(ex, "Failed to download \"{Uri}\" on try {I}: {Message}", Uri, i, ex.Message);
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            lastException = ex;
            Logger?.LogError(ex, "Failed to create download path '{DownloadPath}' due to missing permission: {Message}", DownloadPath, ex.Message);
            var restartManager = Services.GetRequiredService<IRestartManager>();
            restartManager.SetRestart(RestartType.ApplicationElevation);
        }

        return lastException;
    }

    private bool TryReuseCachedDownload()
    {
        var integrity = Component.OriginInfo?.IntegrityInformation;
        if (integrity is null || integrity.Value.HashType == HashTypeKey.None || integrity.Value.Hash is null)
            return false;

        var fileInfo = _fileSystem.FileInfo.New(DownloadPath);

        if (!fileInfo.Exists)
            return false;
       
        
        if (fileInfo.Length != Component.DownloadSize)
            return false;

        try
        {
            var hashing = Services.GetRequiredService<IHashingService>();
            var actual = hashing.GetHash(fileInfo, integrity.Value.HashType);
            if (!actual.SequenceEqual(integrity.Value.Hash))
                return false;
        }
        catch (Exception ex)
        {
            Logger?.LogTrace(ex, "Cached download check failed for '{Path}'; will redownload.", DownloadPath);
            return false;
        }

        Logger?.LogInformation("Reusing cached download at '{Path}'.", DownloadPath);
        return true;
    }

    private async Task DownloadAndVerifyAsync(string destination, CancellationToken token)
    {
        var integrityInformation = Component.OriginInfo!.IntegrityInformation;
        try
        {
#if NETSTANDARD2_1
            await using var file = _fileSystem.FileStream.New(destination, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
#else
            using var file = _fileSystem.FileStream.New(destination, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
#endif
            await downloadManager.DownloadAsync(Uri, file, OnProgress, null,
                new HashDownloadValidator(integrityInformation.Hash, integrityInformation.HashType, Services), token)
                .ConfigureAwait(false);

        }
        catch (OperationCanceledException)
        {
            try
            {
                Logger?.LogTrace(
                    "Deleting potentially partially downloaded file '{FileInfo}' generated as a result of operation cancellation.", destination);
                _fileSystem.File.Delete(destination);
            }
            catch (Exception e)
            {
                Logger?.LogTrace("Could not delete partially downloaded file '{FileInfo}' due to exception: {Exception}", destination, e);
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