using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;

public sealed class InstalledComponentsCatalog(IProductReference product, IReadOnlyCollection<IInstallableComponent> items)
{
    public IProductReference Product { get; } = product ?? throw new ArgumentNullException(nameof(product));

    public IReadOnlyCollection<IInstallableComponent> Components { get; } = items;
}





public sealed class ProductManifestBase
{
    public IProductReference Product { get; }

    public IReadOnlyCollection<IProductComponent> Components { get; }

    public IEnumerable<IInstallableComponent> GetInstallableComponents() =>
        Components.Where(x => x is IInstallableComponent).OfType<IInstallableComponent>();

    public ProductManifestBase(IProductReference product, IEnumerable<IProductComponent> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        Product = product ?? throw new ArgumentNullException(nameof(product));
        Components = items.ToList();
    }
}