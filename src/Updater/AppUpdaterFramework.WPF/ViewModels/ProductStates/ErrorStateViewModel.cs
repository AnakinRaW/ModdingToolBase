using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

public class ErrorStateViewModel : ViewModelBase, IErrorStateViewModel
{
    public string? Version { get; }

    public string ErrorMessage { get; }

    public ErrorStateViewModel(IInstalledProduct installedProduct, string error, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Version = installedProduct.Version?.ToString();
        ErrorMessage = error;
    }
}