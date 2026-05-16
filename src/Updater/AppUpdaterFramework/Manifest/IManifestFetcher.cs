using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Manifest;

internal interface IManifestFetcher
{
    Task<ProductManifest> FetchAsync(ProductReference productReference, CancellationToken cancellationToken);
}
