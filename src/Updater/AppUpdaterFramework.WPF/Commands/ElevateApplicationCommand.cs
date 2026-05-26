using System;
using System.Windows.Input;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Imaging;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Commands;

internal class ElevateApplicationCommand : CommandDefinition
{
    public override string Text => "Restart as Admin";

    public override ICommand Command { get; }

    public override ImageKey Image => UpdaterImageKeys.UACShield;

    public ElevateApplicationCommand(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<IUpdateResultHandler>();
        Command = new AsyncRelayCommand(
            () => handler.Handle(new UpdateResult { RestartType = RestartType.ApplicationElevation }),
            () => true);
    }
}
