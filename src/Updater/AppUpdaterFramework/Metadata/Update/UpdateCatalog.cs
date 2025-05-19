using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Update;

public sealed class UpdateCatalog
{
    public InstalledProduct InstalledProduct { get; }

    public ProductReference UpdateReference { get; }

    public IReadOnlyCollection<UpdateItem> UpdateItems { get; }

    public UpdateCatalogAction Action { get; }

    internal UpdateCatalog(
        InstalledProduct installedProduct,
        ProductReference updateReference,
        IEnumerable<UpdateItem> updateItems)
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

        if (updateCount == UpdateItems.Count && IsNotInstalled(InstalledProduct.Manifest))
            return UpdateCatalogAction.Install;
        
        if (deleteCount == UpdateItems.Count)
            return UpdateCatalogAction.Uninstall;

        if (updateCount > 0 || deleteCount > 0)
            return UpdateCatalogAction.Update;
        
        return UpdateCatalogAction.None;
    }

    private static bool IsNotInstalled(ProductManifest installedProductManifest)
    {
        var currentComponents = installedProductManifest.GetInstallableComponents().ToList();
        return currentComponents.Count == 0 || currentComponents.All(x => x.DetectedState == DetectionState.Absent);
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