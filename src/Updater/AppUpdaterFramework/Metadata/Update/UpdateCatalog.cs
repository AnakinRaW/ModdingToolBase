using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Update;

internal class UpdateCatalog(
    IInstalledProduct installedProduct,
    IProductReference updateReference,
    IEnumerable<IUpdateItem> updateItems,
    UpdateCatalogAction action = UpdateCatalogAction.Update)
    : IUpdateCatalog
{
    public IInstalledProduct InstalledProduct { get; } = installedProduct ?? throw new ArgumentNullException(nameof(installedProduct));

    public IProductReference UpdateReference { get; } = updateReference ?? throw new ArgumentNullException(nameof(updateReference));

    public IReadOnlyCollection<IUpdateItem> UpdateItems { get; } = updateItems.ToList();

    public UpdateCatalogAction Action { get; } = action;

    internal static UpdateCatalog CreateEmpty(IInstalledProduct installedProduct, IProductReference updateReference)
    {
        return new UpdateCatalog(installedProduct, updateReference, Enumerable.Empty<IUpdateItem>(), UpdateCatalogAction.None);
    }
}