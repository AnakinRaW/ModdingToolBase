﻿using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

public abstract class ManifestLoaderBase : IManifestLoader
{
    protected readonly IServiceProvider ServiceProvider;

    protected ManifestLoaderBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public async Task<IProductManifest> LoadManifest(IFileInfo manifestFile, IProductReference productReference, CancellationToken cancellationToken = default)
    {
        Requires.NotNull(manifestFile, nameof(manifestFile));
        Requires.NotNull(productReference, nameof(productReference));
        using var manifest = manifestFile.OpenRead();
        return await LoadManifestCore(manifest, productReference, cancellationToken);
    }

    protected abstract Task<IProductManifest> LoadManifestCore(Stream manifest, IProductReference productReference, CancellationToken cancellationToken);

    protected void ValidateCompatibleManifest(IProductReference manifestProduct, IProductReference installedProduct)
    {
        if (!ProductReferenceEqualityComparer.NameOnly.Equals(manifestProduct, installedProduct))
            throw new CatalogException(
                $"Manifest for '{manifestProduct.Name}' does not match installed product '{installedProduct.Name}' by name.");
    }
}