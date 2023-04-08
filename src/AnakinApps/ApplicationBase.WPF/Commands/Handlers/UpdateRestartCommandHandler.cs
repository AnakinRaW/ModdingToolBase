using System;
using AnakinRaW.ApplicationBase.Update.External;
using AnakinRaW.AppUpdaterFramework.Commands.Handlers;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Commands.Handlers;

internal class UpdateRestartCommandHandler : CommandHandlerBase<RequiredRestartOptionsKind>, IUpdateRestartCommandHandler
{
    private readonly IExternalUpdaterService _updaterService;
    private readonly IApplicationShutdownService _shutdownService;

    public UpdateRestartCommandHandler(IServiceProvider serviceProvider)
    {
        _updaterService = serviceProvider.GetRequiredService<IExternalUpdaterService>();
        _shutdownService = serviceProvider.GetRequiredService<IApplicationShutdownService>();
    }

    public override void Handle(RequiredRestartOptionsKind optionsKind)
    {
        _updaterService.Launch(CreateOptions(optionsKind));
        _shutdownService.Shutdown(0);
    }

    private ExternalUpdaterOptions CreateOptions(RequiredRestartOptionsKind optionsKind)
    {
        return optionsKind switch
        {
            RequiredRestartOptionsKind.Restart => _updaterService.CreateRestartOptions(),
            RequiredRestartOptionsKind.RestartElevated => _updaterService.CreateRestartOptions(true),
            RequiredRestartOptionsKind.Update => _updaterService.CreateUpdateOptions(),
            _ => throw new ArgumentOutOfRangeException(nameof(optionsKind), optionsKind, null)
        };
    }
}