using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

public class ErrorStateViewModel(IInstalledProduct installedProduct, string error, IServiceProvider serviceProvider)
    : ViewModelBase(serviceProvider), IErrorStateViewModel
{
    public string? Version { get; } = installedProduct.Version?.ToString();

    public string ErrorMessage { get; } = error;
}