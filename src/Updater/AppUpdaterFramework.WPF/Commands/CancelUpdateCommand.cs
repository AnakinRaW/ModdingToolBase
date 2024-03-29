using System.Windows.Input;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using AnakinRaW.CommonUtilities.Wpf.Input;

namespace AnakinRaW.AppUpdaterFramework.Commands;

internal class CancelUpdateCommand(IUpdateSession updateSession) : CommandDefinition
{
    public override ImageKey Image => default;
    public override string Text => "Cancel";
    public override ICommand Command { get; } = new DelegateCommand(updateSession.Cancel);
    public override string? Tooltip => null;
}