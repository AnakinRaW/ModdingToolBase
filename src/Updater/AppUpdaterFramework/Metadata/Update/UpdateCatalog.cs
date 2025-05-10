using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Update;

internal sealed class UpdateCatalog : IUpdateCatalog
{
    public IInstalledProduct InstalledProduct { get; }

    public IProductReference UpdateReference { get; }

    public IReadOnlyCollection<IUpdateItem> UpdateItems { get; }

    public UpdateCatalogAction Action { get; }

    public UpdateCatalog(IInstalledProduct installedProduct,
        IProductReference updateReference,
        IEnumerable<IUpdateItem> updateItems)
    {
        InstalledProduct = installedProduct ?? throw new ArgumentNullException(nameof(installedProduct));
        UpdateReference = updateReference ?? throw new ArgumentNullException(nameof(updateReference));
        UpdateItems = updateItems.ToList();
        Action = DetermineAction();
    }

    private UpdateCatalogAction DetermineAction()
    {
        if (UpdateItems.Count == 0)
            return UpdateCatalogAction.None;
        
        var updateCount = 0;
        var deleteCount = 0;
        foreach (var updateItem in UpdateItems)
        {
            if (updateItem.Action == UpdateAction.Update)
                updateCount++;
            if (updateItem.Action == UpdateAction.Delete)
                deleteCount++;
        }

        if (updateCount == UpdateItems.Count)
            return UpdateCatalogAction.Install;
        if (deleteCount == UpdateItems.Count)
            return UpdateCatalogAction.Uninstall;

        if (updateCount > 0 || deleteCount > 0)
            return UpdateCatalogAction.Update;
        
        return UpdateCatalogAction.None;
    }


    internal static UpdateCatalog CreateEmpty(IInstalledProduct installedProduct, IProductReference updateReference)
    {
        return new UpdateCatalog(installedProduct, updateReference, []);
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