using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal interface IUpdateCatalogProvider
{
    UpdateCatalog Create(InstalledProduct installedProduct, ProductManifest availableManifest);
}