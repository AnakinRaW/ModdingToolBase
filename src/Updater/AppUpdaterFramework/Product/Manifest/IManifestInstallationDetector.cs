using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

internal interface IManifestInstallationDetector
{
    void DetectInstalledComponents(ProductManifest catalog, IReadOnlyDictionary<string, string> productVariables);
}