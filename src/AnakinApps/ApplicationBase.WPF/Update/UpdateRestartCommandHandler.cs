using System;
using AnakinRaW.ApplicationBase.Update.External;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.AppUpdaterFramework.Updater.Handlers;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

internal class UpdateRestartCommandHandler : IRestartHandler
{
    private readonly IExternalUpdaterService _externalUpdaterService;
    private readonly IApplicationShutdownService _shutdownService;

    public UpdateRestartCommandHandler(IServiceProvider serviceProvider)
    {
        _externalUpdaterService = serviceProvider.GetRequiredService<IExternalUpdaterService>();
        _shutdownService = serviceProvider.GetRequiredService<IApplicationShutdownService>();
    }

    public void Restart(RequiredRestartOptionsKind optionsKind)
    {
        _externalUpdaterService.Launch(CreateOptions(optionsKind));
        _shutdownService.Shutdown(0);
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