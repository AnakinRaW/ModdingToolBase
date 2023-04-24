using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

public interface IProductUpdateInformation
{
    bool IsUpdateAvailable { get; }

    IUpdateCatalog? UpdateCatalog { get; }
}