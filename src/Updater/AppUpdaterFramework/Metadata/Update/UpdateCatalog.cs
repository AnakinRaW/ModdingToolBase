using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Update;

internal sealed class UpdateCatalog(
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
        return new UpdateCatalog(installedProduct, updateReference, [], UpdateCatalogAction.None);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"InstalledProduct='{InstalledProduct}', Action='{Action}'");
        sb.AppendLine("\t[");
        foreach (var updateItem in UpdateItems)
        {
            sb.AppendLine("\t\t{" + updateItem + "},");
        }
        sb.AppendLine("\t]");
        sb.AppendLine("}");
        return sb.ToString();
    }
}