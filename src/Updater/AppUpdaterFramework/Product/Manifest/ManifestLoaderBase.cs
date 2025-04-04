using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

public abstract class ManifestLoaderBase(IServiceProvider serviceProvider) : IManifestLoader
{
    protected readonly IServiceProvider ServiceProvider = serviceProvider;

    public Task<IProductManifest> LoadManifestAsync(
        Uri manifestUri, 
        IProductReference productReference,
        CancellationToken cancellationToken = default)
    {
        if (manifestUri == null)
            throw new ArgumentNullException(nameof(manifestUri));
        if (productReference == null)
            throw new ArgumentNullException(nameof(productReference));
        return LoadManifestCoreAsync(manifestUri, productReference, cancellationToken);
    }

    protected abstract Task<IProductManifest> LoadManifestCoreAsync(
        Uri manifestUri,
        IProductReference productReference,
        CancellationToken cancellationToken = default);

    protected void ValidateCompatibleManifest(IProductReference manifestProduct, IProductReference installedProduct)
    {
        if (!ProductReferenceEqualityComparer.NameOnly.Equals(manifestProduct, installedProduct))
            throw new CatalogException(
                $"Manifest for '{manifestProduct.Name}' does not match installed product '{installedProduct.Name}' by name.");
    }
}