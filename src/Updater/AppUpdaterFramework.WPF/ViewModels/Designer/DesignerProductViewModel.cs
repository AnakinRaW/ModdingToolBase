using System.ComponentModel;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.Designer;

// ReSharper disable All 
#pragma warning disable CS0067
[EditorBrowsable(EditorBrowsableState.Never)]
public class DesignerProductViewModel : IProductViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayName => "Test Product";
    public ImageKey Icon { get; }
    public ICommandDefinition? Action { get; }
    public IProductStateViewModel StateViewModel { get; } = new DesignerUpdateAvailableStateViewModel();

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}