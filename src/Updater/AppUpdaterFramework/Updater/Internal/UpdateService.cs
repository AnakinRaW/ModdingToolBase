using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class UpdateService : IUpdateService
{
    public event EventHandler? CheckingForUpdatesStarted;
    public event EventHandler<UpdateCatalog?>? CheckingForUpdatesCompleted;
    public event EventHandler<IUpdateSession>? UpdateStarted;
    public event EventHandler<UpdateResult?>? UpdateCompleted;

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger? _logger;

    private readonly object _syncLock = new();

    public bool IsCheckingForUpdates { get; private set; }

    public bool IsUpdating { get; private set; }

    public UpdateService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public async Task<UpdateCatalog?> CheckForUpdatesAsync(ProductReference productReference, CancellationToken token = default)
    {
        lock (_syncLock)
        {
            if (IsCheckingForUpdates || IsUpdating)
                return null;
            IsCheckingForUpdates = true;
        }

        CheckingForUpdatesStarted?.RaiseAsync(this, EventArgs.Empty);

        UpdateCatalog? updateCatalog = null;

        try
        {
            if (productReference.Branch is null)
                throw new ManifestException("Product reference does not have a branch.");

            var fetcher = _serviceProvider.GetRequiredService<IManifestFetcher>();
            var manifest = await fetcher.FetchAsync(productReference, token).ConfigureAwait(false);

            var productService = _serviceProvider.GetRequiredService<IProductService>();
            
            productService.UpdateComponentDetectionState();
            
            var currentInstance = productService.GetCurrentInstance();

            var updateCatalogBuilder = new UpdateCatalogFactory(_serviceProvider);
            updateCatalog = updateCatalogBuilder.Create(currentInstance, manifest);

            return updateCatalog;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
            throw;
        }
        finally
        {
            lock (_syncLock) 
                IsCheckingForUpdates = false;
            CheckingForUpdatesCompleted?.RaiseAsync(this, updateCatalog);
        }
    }

    public async Task<UpdateResult?> UpdateAsync(ProductManifest manifest, CancellationToken token = default)
    {
        if (manifest is null)
            throw new ArgumentNullException(nameof(manifest));

        var productService = _serviceProvider.GetRequiredService<IProductService>();
        productService.UpdateComponentDetectionState();
        var currentInstance = productService.GetCurrentInstance();
        var catalog = new UpdateCatalogFactory(_serviceProvider).Create(currentInstance, manifest);
        return await UpdateAsync(catalog, token).ConfigureAwait(false);
    }

    public async Task<UpdateResult?> UpdateAsync(UpdateCatalog updateCatalog, CancellationToken token = default)
    {
        lock (_syncLock)
        {
            if (IsUpdating)
                return null;
            IsUpdating = true;
        }

        UpdateResult? updateResult = null;
        try
        {
            var updateSession = new UpdateSession(updateCatalog.UpdateReference,
                new ApplicationUpdater(updateCatalog, _serviceProvider));

            UpdateStarted?.Invoke(this, updateSession);
            updateResult = await updateSession.StartUpdate(token);
            return updateResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
            throw;
        }
        finally
        {
            lock (_syncLock)
                IsUpdating = false;
            UpdateCompleted?.Invoke(this, updateResult);
        }
    }
}