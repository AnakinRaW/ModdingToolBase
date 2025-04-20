using System;
using System.Windows.Input;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using AnakinRaW.AppUpdaterFramework.Imaging;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using AnakinRaW.CommonUtilities.Wpf.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Commands;

internal class ElevateApplicationCommand : CommandDefinition
{
    public override string Text => "Restart as Admin";

    public override ICommand Command { get; }

    public override ImageKey Image => UpdaterImageKeys.UACShield;

    public ElevateApplicationCommand(IServiceProvider serviceProvider)
    {
        var handler = serviceProvider.GetRequiredService<IRestartHandler>();
        var options = RequiredRestartOptionsKind.RestartElevated;
        Command = new DelegateCommand(() => handler.Restart(options), () => true);
    }
}