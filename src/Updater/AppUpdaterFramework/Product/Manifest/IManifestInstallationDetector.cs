using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

internal interface IManifestInstallationDetector
{
    IReadOnlyCollection<IInstallableComponent> DetectInstalledComponents(
        IProductManifest catalog, 
        IReadOnlyDictionary<string, string> productVariables);
}