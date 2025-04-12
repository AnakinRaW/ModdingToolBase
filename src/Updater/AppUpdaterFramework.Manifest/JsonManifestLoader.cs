using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.CommonUtilities.DownloadManager;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.AppUpdaterFramework;

public sealed class JsonManifestLoader(IServiceProvider serviceProvider) : ManifestLoaderBase(serviceProvider)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ValueTask<ApplicationManifest?> DeserializeAsync(Stream stream, CancellationToken token = default)
    {
        return JsonSerializer.DeserializeAsync<ApplicationManifest>(stream, JsonSerializerOptions, token);
    }

    protected override async Task<IProductManifest> LoadManifestCoreAsync(
        Uri manifestUri, 
        IProductReference productReference,
        DownloadOptions? downloadOptions,
        CancellationToken cancellationToken = default)
    {
        using var manifestFileLoader = new ManifestFileDownloader(ServiceProvider);

        var manifestFile = await manifestFileLoader.DownloadManifestAsync(manifestUri, downloadOptions, cancellationToken);

        using var manifestFileStream = manifestFile.OpenRead();

        var appManifest = await DeserializeAsync(manifestFileStream, cancellationToken).ConfigureAwait(false);
        if (appManifest is null)
            throw new CatalogException("Serialized manifest is null");

        var availProduct = BuildReference(appManifest);
        ValidateCompatibleManifest(availProduct, productReference);
        var catalog = BuildCatalog(appManifest.Components);
        return new ProductManifest(availProduct, catalog);
    }

    private IProductReference BuildReference(ApplicationManifest applicationManifest)
    {
        SemVersion? version = null;
        if (applicationManifest.Version is not null)
            version = SemVersion.Parse(applicationManifest.Version, SemVersionStyles.Any);

        ProductBranch? branch = null;
        if (version is not null && applicationManifest.Branch is not null)
        {
            var branchManager = ServiceProvider.GetRequiredService<IBranchManager>();
            branch = branchManager.GetBranchFromVersion(version);
        }
        return new ProductReference(applicationManifest.Name, version, branch);
    }

    private static IReadOnlyList<IProductComponent> BuildCatalog(IEnumerable<AppComponent> manifestComponents)
    {
        var catalog = new List<IProductComponent>();
        foreach (var manifestComponent in manifestComponents)
        {
            switch (manifestComponent.Type)
            {
                case ComponentType.File:
                    catalog.Add(manifestComponent.ToInstallable());
                    break;
                case ComponentType.Group:
                    catalog.Add(manifestComponent.ToGroup());
                    break;
                default:
                    throw new InvalidOperationException($"{manifestComponent.Type} is not supported.");
            }
        }
        return catalog;
    }
}