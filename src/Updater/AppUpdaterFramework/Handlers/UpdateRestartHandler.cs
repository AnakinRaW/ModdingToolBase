using System;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Handlers;


// TODO: Make Shutdown event
internal sealed class UpdateRestartHandler(IServiceProvider serviceProvider) : IRestartHandler
{
    private readonly IExternalUpdaterService _externalUpdaterService = serviceProvider.GetRequiredService<IExternalUpdaterService>();
    private readonly UpdateConfiguration _updateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();

    public void Restart(RequiredRestartOptionsKind optionsKind)
    {
        if (!_updateConfiguration.RestartConfiguration.SupportsRestart)
            throw new NotSupportedException("Restarting the application is not supported.");

        var restartOptions = CreateOptions(optionsKind);
        _externalUpdaterService.Launch(restartOptions);
        Shutdown();
    }

    private void Shutdown()
    {
        Environment.Exit(RestartConstants.RestartRequiredCode);
    }

    private ExternalUpdaterOptions CreateOptions(RequiredRestartOptionsKind optionsKind)
    {
        return optionsKind switch
        {
            RequiredRestartOptionsKind.Restart => _externalUpdaterService.CreateRestartOptions(),
            RequiredRestartOptionsKind.RestartElevated => _externalUpdaterService.CreateRestartOptions(true),
            RequiredRestartOptionsKind.Update => _externalUpdaterService.CreateUpdateOptions(),
            _ => throw new ArgumentOutOfRangeException(nameof(optionsKind), optionsKind, null)
        };
    }
}