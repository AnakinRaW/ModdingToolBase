using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

internal readonly struct ProductUpdateInformation : IProductUpdateInformation
{
    public ProductUpdateInformation(IUpdateCatalog? updateCatalog)
    {
        UpdateCatalog = updateCatalog;
    }

    public bool IsUpdateAvailable => UpdateCatalog is not null;

    public IUpdateCatalog? UpdateCatalog { get; }
}