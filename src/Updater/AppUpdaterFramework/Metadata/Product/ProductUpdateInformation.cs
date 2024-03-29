using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

internal readonly struct ProductUpdateInformation(IUpdateCatalog? updateCatalog) : IProductUpdateInformation
{
    public bool IsUpdateAvailable => UpdateCatalog is not null;

    public IUpdateCatalog? UpdateCatalog { get; } = updateCatalog;
}