using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

public interface IUpdateAvailableStateViewModel : IProductStateViewModel
{
    UpdateCatalog UpdateCatalog { get; }

    string? CurrentVersion { get; }

    string? AvailableVersion { get; }
}