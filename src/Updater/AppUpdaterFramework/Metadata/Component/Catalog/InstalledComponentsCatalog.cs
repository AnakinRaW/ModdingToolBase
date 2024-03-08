using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;

internal class InstalledComponentsCatalog(IProductReference product, IReadOnlyCollection<IInstallableComponent> items)
    : IInstalledComponentsCatalog
{
    public IProductReference Product { get; } = product ?? throw new ArgumentNullException(nameof(product));

    public IReadOnlyCollection<IInstallableComponent> Items { get; } = items;
}