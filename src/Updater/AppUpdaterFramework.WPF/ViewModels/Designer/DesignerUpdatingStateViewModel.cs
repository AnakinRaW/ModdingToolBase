using System.ComponentModel;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;
using AnakinRaW.AppUpdaterFramework.ViewModels.Progress;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.Designer;

// ReSharper disable All 
#pragma warning disable CS0067
[EditorBrowsable(EditorBrowsableState.Never)]
internal class DesignerUpdatingStateViewModel : IUpdatingStateViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public Task InitializeAsync()
    {
        throw new System.NotImplementedException();
    }

    public IProgressViewModel ProgressViewModel { get; } = null!;
}