using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Manifest;

public sealed class ProductManifest : IEnumerable<ProductComponent>
{
    public ProductReference Product { get; }

    public IReadOnlyCollection<ProductComponent> Components { get; }

    public IEnumerable<InstallableComponent> GetInstallableComponents() =>
        Components.Where(x => x is InstallableComponent).OfType<InstallableComponent>();

    public ProductManifest(ProductReference product, IEnumerable<ProductComponent> components)
    {
        if (components == null)
            throw new ArgumentNullException(nameof(components));
        Product = product ?? throw new ArgumentNullException(nameof(product));
        Components = components.ToList();
    }

    public IEnumerator<ProductComponent> GetEnumerator()
    {
        return Components.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}