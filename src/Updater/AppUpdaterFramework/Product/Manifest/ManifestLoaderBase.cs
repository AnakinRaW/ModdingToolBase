using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.CommonUtilities.DownloadManager;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

public abstract class ManifestLoaderBase(IServiceProvider serviceProvider) : IManifestLoader
{
    protected readonly IServiceProvider ServiceProvider = serviceProvider;

    public async Task<ProductManifest> LoadManifestAsync(
        Uri manifestUri, 
        ProductReference productReference,
        DownloadOptions? downloadOptions,
        CancellationToken cancellationToken = default)
    {
        if (manifestUri == null)
            throw new ArgumentNullException(nameof(manifestUri));
        if (productReference == null)
            throw new ArgumentNullException(nameof(productReference));
        var manifest = await LoadManifestCoreAsync(manifestUri, productReference, downloadOptions, cancellationToken).ConfigureAwait(false);
        ValidateCompatibleManifest(manifest.Product, productReference);
        return manifest;
    }

    protected abstract Task<ProductManifest> LoadManifestCoreAsync(
        Uri manifestUri,
        ProductReference productReference,
        DownloadOptions? downloadOptions,
        CancellationToken cancellationToken = default);

    protected static void ValidateCompatibleManifest(ProductReference manifestProduct, ProductReference installedProduct)
    {
        if (!ProductReferenceEqualityComparer.NameOnly.Equals(manifestProduct, installedProduct))
            throw new ManifestException(
                $"Manifest for '{manifestProduct.Name}' does not match installed product '{installedProduct.Name}' by name.");
    }
}