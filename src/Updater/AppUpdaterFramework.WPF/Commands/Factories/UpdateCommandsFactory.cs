using System;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;

namespace AnakinRaW.AppUpdaterFramework.Commands.Factories;

internal class UpdateCommandsFactory(IServiceProvider serviceProvider) : IUpdateCommandsFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public ICommandDefinition CreateRestart()
    {
        return new UpdateRestartCommand(_serviceProvider);
    }

    public ICommandDefinition CreateElevate()
    {
        return new ElevateApplicationCommand(_serviceProvider);
    }
}