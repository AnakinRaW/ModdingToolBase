using System;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

internal class UpdateRestartHandler : IRestartHandler
{
    private readonly IExternalUpdaterService _externalUpdaterService;

    public UpdateRestartHandler(IServiceProvider serviceProvider)
    {
        _externalUpdaterService = serviceProvider.GetRequiredService<IExternalUpdaterService>();
    }

    public void Restart(RequiredRestartOptionsKind optionsKind)
    {
        _externalUpdaterService.Launch(CreateOptions(optionsKind));
        Shutdown();
    }

    protected virtual void Shutdown()
    {
        Environment.Exit(RestartConstants.RestartRequiredCode);
    }

    protected virtual ExternalUpdaterOptions CreateOptions(RequiredRestartOptionsKind optionsKind)
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