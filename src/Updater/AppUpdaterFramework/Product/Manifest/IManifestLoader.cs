using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

public interface IManifestLoader
{
    Task<IProductManifest> LoadManifest(IFileInfo manifestFile, IProductReference productReference, CancellationToken cancellationToken = default);
}