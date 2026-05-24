using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Updater;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.Factories;

internal interface IProductViewModelFactory
{
    IProductViewModel Create(InstalledProduct product, UpdateCatalog? updateCatalog);

    IProductViewModel Create(IUpdateSession updateSession);
}