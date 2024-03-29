using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

public class UpdateAvailableStateViewModel(
    IInstalledProduct installedProduct,
    IUpdateCatalog updateCatalog,
    IServiceProvider serviceProvider)
    : ViewModelBase(serviceProvider), IUpdateAvailableStateViewModel
{
    public IUpdateCatalog UpdateCatalog { get; } = updateCatalog ?? throw new ArgumentNullException(nameof(updateCatalog));

    public string? CurrentVersion { get; } = installedProduct.Version?.ToString();

    public string? AvailableVersion { get; } = updateCatalog.UpdateReference.Version?.ToString();
}