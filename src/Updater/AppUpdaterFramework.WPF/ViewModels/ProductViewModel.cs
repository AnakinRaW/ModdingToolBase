using System;
using AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;
using AnakinRaW.CommonUtilities.Wpf.Imaging;

namespace AnakinRaW.AppUpdaterFramework.ViewModels;

public class ProductViewModel(
    string displayName,
    ImageKey icon,
    IProductStateViewModel stateViewModel,
    ICommandDefinition? action,
    IServiceProvider serviceProvider)
    : ViewModelBase(serviceProvider), IProductViewModel
{
    public string DisplayName { get; } = displayName;

    public ImageKey Icon { get; } = icon;

    public IProductStateViewModel StateViewModel { get; } = stateViewModel ?? throw new ArgumentNullException(nameof(stateViewModel));

    public ICommandDefinition? Action { get; } = action;
}