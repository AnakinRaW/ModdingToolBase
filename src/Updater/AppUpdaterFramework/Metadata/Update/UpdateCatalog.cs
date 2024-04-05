using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Update;

internal sealed class UpdateCatalog : IUpdateCatalog
{
    public UpdateCatalog(IInstalledProduct installedProduct,
        IProductReference updateReference,
        IEnumerable<IUpdateItem> updateItems,
        UpdateCatalogAction action = UpdateCatalogAction.Update)
    {
        InstalledProduct = installedProduct ?? throw new ArgumentNullException(nameof(installedProduct));
        UpdateReference = updateReference ?? throw new ArgumentNullException(nameof(updateReference));
        UpdateItems = updateItems.ToList();
        Action = action;
    }

    public IInstalledProduct InstalledProduct { get; }

    public IProductReference UpdateReference { get; }

    public IReadOnlyCollection<IUpdateItem> UpdateItems { get; }

    public UpdateCatalogAction Action { get; }

    internal static UpdateCatalog CreateEmpty(IInstalledProduct installedProduct, IProductReference updateReference)
    {
        return new UpdateCatalog(installedProduct, updateReference, Enumerable.Empty<IUpdateItem>(), UpdateCatalogAction.None);
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