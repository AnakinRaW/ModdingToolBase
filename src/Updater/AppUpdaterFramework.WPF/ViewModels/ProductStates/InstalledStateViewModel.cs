using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

public class InstalledStateViewModel(IInstalledProduct installedProduct, IServiceProvider serviceProvider)
    : ViewModelBase(serviceProvider), IInstalledStateViewModel
{
    public string? Version { get; } = installedProduct.Version?.ToString();
}