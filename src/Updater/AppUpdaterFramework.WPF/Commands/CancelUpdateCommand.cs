using System.Windows.Input;
using AnakinRaW.AppUpdaterFramework.Updater;

namespace AnakinRaW.AppUpdaterFramework.Commands;

internal class CancelUpdateCommand : CommandDefinition
{
    public override ImageKey Image => default;
    public override string Text => "Cancel";
    public override ICommand Command { get; }
    public override string? Tooltip => null;

    public CancelUpdateCommand(IUpdateSession updateSession)
    {
        Command = new DelegateCommand(updateSession.Cancel);
    }
}