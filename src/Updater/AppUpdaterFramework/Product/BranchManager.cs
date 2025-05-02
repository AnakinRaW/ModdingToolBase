using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.DownloadManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Product;

public abstract class BranchManagerBase : IBranchManager
{
    private readonly ILogger? _logger;
    private readonly IManifestLoader _manifestLoader;

    public abstract string StableBranchName { get; }

    protected BranchManagerBase(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _manifestLoader = serviceProvider.GetRequiredService<IManifestLoader>();
    }

    public ProductBranch GetBranchFromName(string branchName)
    {
        ThrowHelper.ThrowIfNullOrEmpty(branchName);
        var isDefault = ProductBranch.BranchNamEqualityComparer.Equals(branchName, StableBranchName);
        return new ProductBranch(branchName, isDefault);
    }

    public abstract Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync();

    public async Task<IProductManifest> GetManifestAsync(IProductReference productReference, CancellationToken token = default)
    {
        if (productReference == null) 
            throw new ArgumentNullException(nameof(productReference));
        token.ThrowIfCancellationRequested();
        if (productReference.Branch is null)
            throw new InvalidOperationException("No branch specified.");

        var branch = productReference.Branch;
        
        Exception? lastException = null;


        if (branch.ManifestLocations.Count == 0)
            throw new CatalogDownloadException("No location to an update manifest specified.");

        foreach (var manifestLocation in branch.ManifestLocations)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                var downloadOption = GetDownloadOptionsForManifestDownload(manifestLocation, productReference);
                return await _manifestLoader.LoadManifestAsync(manifestLocation, productReference, downloadOption, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, ex.Message);
                lastException = ex;
            }
        }

        var message = $"Unable to get manifest of branch: '{branch.Name}'";
        _logger?.LogError(lastException, message);
        throw new CatalogDownloadException("Could not download branch manifest from all sources.", lastException!);
    }
    
    protected abstract ICollection<Uri> BuildManifestUris(string branchName);

    protected virtual DownloadOptions? GetDownloadOptionsForManifestDownload(Uri manifestUri, IProductReference productReference)
    {
        return null;
    }
}