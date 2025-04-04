using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

public interface IManifestLoader
{
    Task<IProductManifest> LoadManifestAsync(
        Uri manifestUri,
        IProductReference productReference, 
        CancellationToken cancellationToken = default);
}