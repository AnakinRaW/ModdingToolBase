using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.CommonUtilities.DownloadManager;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

public interface IManifestLoader
{
    Task<ProductManifest> LoadManifestAsync(
        Uri manifestUri,
        ProductReference productReference, 
        DownloadOptions? downloadOptions,
        CancellationToken cancellationToken = default);
}