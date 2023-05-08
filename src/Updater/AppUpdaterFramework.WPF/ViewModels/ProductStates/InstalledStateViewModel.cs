using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

public class InstalledStateViewModel : ViewModelBase, IInstalledStateViewModel
{
    public string? Version { get; }

    public InstalledStateViewModel(IInstalledProduct installedProduct, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Version = installedProduct.Version?.ToString();
    }
}