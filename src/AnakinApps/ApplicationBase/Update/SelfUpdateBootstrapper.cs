using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AnakinRaW.ApplicationBase.Update;

internal sealed class SelfUpdateBootstrapper : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger? _logger;
    private readonly UpdatableApplicationEnvironment _applicationEnvironment;
    private readonly ApplicationUpdateRegistry _updateRegistry;
    private readonly IPendingUpdate _pendingUpdate;

    public SelfUpdateBootstrapper(UpdatableApplicationEnvironment applicationEnvironment, IServiceProvider services)
    {
        _applicationEnvironment = applicationEnvironment;
        _serviceProvider = services;
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        var registry = services.GetRequiredService<IRegistry>();
        _updateRegistry = new ApplicationUpdateRegistry(registry, _applicationEnvironment);
        _pendingUpdate = services.GetRequiredService<IPendingUpdate>();
    }

    public async Task<SelfUpdateResult> UpdateAsync(string[] args)
    {
        if (!_applicationEnvironment.UpdateConfiguration.RestartConfiguration.SupportsRestart)
            return SelfUpdateResult.None;

        if (ExternalUpdaterResultOptions.TryParse(args, out var externalUpdaterResult))
        {
            HandleExternalUpdaterResult(externalUpdaterResult.Result, out var resetApplication);
            if (resetApplication)
                return SelfUpdateResult.Reset;
        }

        if (_updateRegistry.RequiresUpdate)
        {
            _logger?.LogDebug("Registry indicates an update is pending; attempting to resume...");
            try
            {
                var updateResult = await ResumePendingUpdateAsync().ConfigureAwait(false);

                if (updateResult is not null)
                {
                    var resultHandler = new UpdateResultHandler(_serviceProvider);
                    await resultHandler.Handle(updateResult).ConfigureAwait(false);
                }

                // Handle() terminates the application on the restart/elevation/restore-failed paths,
                // so reaching this point means the resume completed in-process (or there was nothing
                // to resume). Either way, the pending state is no longer needed.
                ClearAllPendingState();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to resume pending update: {Message}", e.Message);
                ClearAllPendingState();
            }
        }

        return SelfUpdateResult.Success;
    }

    private void HandleExternalUpdaterResult(ExternalUpdaterResult result, out bool shouldReset)
    {
        shouldReset = false;
        _logger?.LogTrace("ExternalUpdater result: '{Result}'", result);

        if (result == ExternalUpdaterResult.UpdateFailedNoRestore || _updateRegistry.ResetApp)
        {
            _logger?.LogDebug("Resetting app due to ExternalUpdater result '{Result}' or UpdateRegistry/ResetApp = {Reset}", result, _updateRegistry.ResetApp);
            ClearAllPendingState();
            shouldReset = true;
            return;
        }

        if (result == ExternalUpdaterResult.UpdateSuccess)
        {
            ClearAllPendingState();
            return;
        }

        if (result == ExternalUpdaterResult.UpdateFailedWithRestore)
        {
            _logger?.LogDebug("ExternalUpdater restored backups; clearing pending state.");
            ClearAllPendingState();
        }
    }

    private async Task<UpdateResult?> ResumePendingUpdateAsync()
    { 
        if (!TryReadAndVerifyPendingManifest(out var manifest))
            return null;

        var updateService = _serviceProvider.GetRequiredService<IUpdateService>();
        var result = await updateService.UpdateAsync(manifest).ConfigureAwait(false);

        _logger?.LogInformation("Update finished with RestartType={RestartType}", result.RestartType);

        return result;
    }
    
    private bool TryReadAndVerifyPendingManifest([NotNullWhen(true)] out ProductManifest? manifest)
    {
        manifest = null;
        if (!_pendingUpdate.TryRestoreManifestFromDisk())
        {
            _logger?.LogDebug("No pending-update manifest found on disk.");
            return false;
        }

        var bytes = _pendingUpdate.FetchedManifestBytes;
        if (bytes is null)
            return false;

        try
        {
            var loader = _serviceProvider.GetRequiredService<IManifestLoaderProvider>().Loader;
            using var stream = new MemoryStream(bytes, writable: false);
            manifest = loader.LoadAndVerifyManifest(stream);
            return true;
        }
        catch (SignatureVerificationFailedException sigEx)
        {
            _logger?.LogError(sigEx, "Pending manifest failed signature verification ({Result}).",
                sigEx.Result);
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to parse pending manifest: {Message}", ex.Message);
            return false;
        }
    }

    private void ClearAllPendingState()
    {
        _updateRegistry.Reset();
        _pendingUpdate.Clear();
    }

    public void Dispose()
    {
        _updateRegistry.Dispose();
    }
}
