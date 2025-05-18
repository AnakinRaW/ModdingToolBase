using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal interface IUpdateCatalogProvider
{
    IUpdateCatalog Create(IInstalledProduct installedProduct, ProductManifest availableManifest);
}