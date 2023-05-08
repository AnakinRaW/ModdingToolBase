using AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

namespace AnakinRaW.AppUpdaterFramework.ViewModels;

public interface IProductViewModel : IViewModel
{
    string DisplayName { get; }

    ImageKey Icon { get; }

    ICommandDefinition? Action { get; }

    IProductStateViewModel StateViewModel { get; }
}