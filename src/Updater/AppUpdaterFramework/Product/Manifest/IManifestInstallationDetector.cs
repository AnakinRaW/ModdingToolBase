using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

internal interface IManifestInstallationDetector
{
    void DetectInstalledComponents(IProductManifest catalog, IReadOnlyDictionary<string, string> productVariables);
}