using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;

public sealed class ProductManifest(IProductReference product, IReadOnlyCollection<IProductComponent> components)
    : IProductManifest
{
    public IProductReference Product { get; } = product ?? throw new ArgumentNullException(nameof(product));

    public IReadOnlyCollection<IProductComponent> Components { get; } = components ?? throw new ArgumentNullException(nameof(components));
}