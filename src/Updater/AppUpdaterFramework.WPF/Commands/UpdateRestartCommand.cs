using System;
using System.Windows.Input;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Commands;

internal class UpdateRestartCommand : CommandDefinition
{
    public override ImageKey Image => default;
    public override string Text => "Restart";
    public override ICommand Command { get; }
    public override string? Tooltip => null;

    public UpdateRestartCommand(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<IUpdateResultHandler>();
        Command = new AsyncRelayCommand(
            () => handler.Handle(new UpdateResult { RestartType = RestartType.ApplicationRestart }),
            () => true);
    }
}
