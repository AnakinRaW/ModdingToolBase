using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class ApplicationUpdater(UpdateCatalog updateCatalog, IServiceProvider serviceProvider)
    : IApplicationUpdater, IComponentProgressReporter
{
    public event EventHandler<UpdateProgressEventArgs>? Progress;

    private readonly UpdateCatalog _updateCatalog = updateCatalog ?? throw new ArgumentNullException(nameof(updateCatalog));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(ApplicationUpdater));

    public void Report(double progress, string? progressText, ProgressType type, ComponentProgressInfo detailedProgress)
    {
        ThrowHelper.ThrowIfNullOrEmpty(progressText);
        Progress?.Invoke(this, new UpdateProgressEventArgs
        {
            Component = progressText,
            DetailedProgress = detailedProgress,
            Progress = progress,
            Type = type
        });
    }

    public async Task<UpdateResult> UpdateAsync(CancellationToken token)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            var updateResult = await UpdateCoreAsync(token).ConfigureAwait(false);

            if (updateResult.RestartType == RestartType.ApplicationRestart)
                return updateResult;

            if (updateResult.Exception is not null)
                await RestoreBackups().ConfigureAwait(false);

            try
            {
                await CleanUpdateData();
            }
            catch (Exception e)
            {
                _logger?.LogTrace(e, "Failed to clean update data: {Message}", e.Message);
            }
            
            return updateResult;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Update operation failed with error: {Message}", e.Message);
            return CreateResult(e);
        }
    }

    private async Task<UpdateResult> UpdateCoreAsync(CancellationToken token)
    {
        try
        {
            if (!_updateCatalog.UpdateItems.Any())
                throw new InvalidOperationException("Nothing to update!");

            using var updatePipeline = new UpdatePipeline(_updateCatalog, this, _serviceProvider);
            try
            {
                _logger?.LogTrace("Updating...\r\nCatalog: {UpdateCatalog}", _updateCatalog);
                await updatePipeline.RunAsync(token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed update: " + e.Message);
                throw;
            }
            finally
            {
                _logger?.LogTrace("Completed update");
            }
            
            return CreateResult();
        }
        catch (OperationCanceledException e)
        {
            _logger?.LogTrace("User canceled the update.");
            return CreateResult(e);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Update operation failed with error: {Message}", e.Message);
            return CreateResult(e);
        }
    }

    private Task RestoreBackups()
    {
        return Task.Run(async () =>
        {
            try
            {
                var backupManager = _serviceProvider.GetRequiredService<IBackupManager>();
                backupManager.RestoreAll();
            }
            catch (Exception ex)
            {
                _serviceProvider.GetRequiredService<IWritablePendingComponentStore>().Clear();
                _serviceProvider.GetRequiredService<IRestartManager>().SetRestart(RestartType.ApplicationRestart);

               var e = new FailedRestoreException(ex);
                _logger?.LogTrace(e, "Failed to restore from failed update : {Message}", e.Message);
                throw e;
            }
            finally
            {
                await CleanUpdateData();
            }
        });
    }

    private async Task CleanUpdateData()
    {
        await new UpdateCleanPipeline(_serviceProvider).RunAsync().ConfigureAwait(false);
    }

    private UpdateResult CreateResult(Exception? exception = null)
    {
        var restartType = _serviceProvider.GetRequiredService<IRestartManager>().RequiredRestartType;
        var result = new UpdateResult
        {
            Exception = exception,
            IsCanceled = exception?.IsOperationCanceledException() ?? false,
            RestartType = restartType,
            FailedRestore = exception?.IsExceptionType<FailedRestoreException>() ?? false
        };
        return result;
    }
}