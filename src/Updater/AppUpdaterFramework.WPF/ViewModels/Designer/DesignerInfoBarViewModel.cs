using System.ComponentModel;
using System.Threading.Tasks;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.Designer;

// ReSharper disable All 
#pragma warning disable CS0067
[EditorBrowsable(EditorBrowsableState.Never)]
public class DesignerInfoBarViewModel : IUpdateInfoBarViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public UpdateStatus Status { get; set; }
    public string Text => "Status";
    public bool IsCheckingForUpdates => true;

    public void Dispose()
    {
    }
}