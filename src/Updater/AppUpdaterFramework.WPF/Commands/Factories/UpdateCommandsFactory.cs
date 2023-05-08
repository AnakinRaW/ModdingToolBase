using System;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Commands.Factories;

internal class UpdateCommandsFactory : IUpdateCommandsFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UpdateCommandsFactory(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _serviceProvider = serviceProvider;
    }

    public ICommandDefinition CreateRestart()
    {
        return new UpdateRestartCommand(_serviceProvider);
    }

    public ICommandDefinition CreateElevate()
    {
        return new ElevateApplicationCommand(_serviceProvider);
    }
}