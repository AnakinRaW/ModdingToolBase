using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Manifest;

public sealed class ProductManifest : IEnumerable<IProductComponent>
{
    public IProductReference Product { get; }

    public IReadOnlyCollection<IProductComponent> Components { get; }

    public IEnumerable<IInstallableComponent> GetInstallableComponents() =>
        Components.Where(x => x is IInstallableComponent).OfType<IInstallableComponent>();

    public ProductManifest(IProductReference product, IEnumerable<IProductComponent> components)
    {
        if (components == null) throw new ArgumentNullException(nameof(components));
        Product = product ?? throw new ArgumentNullException(nameof(product));
        Components = components.ToList();
    }

    public IEnumerator<IProductComponent> GetEnumerator()
    {
        return Components.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}