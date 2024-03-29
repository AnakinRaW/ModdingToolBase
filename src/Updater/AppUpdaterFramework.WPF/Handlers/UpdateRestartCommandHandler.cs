using System;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

internal class UpdateRestartCommandHandler(IServiceProvider serviceProvider) : UpdateRestartHandler(serviceProvider)
{
    private readonly IApplicationShutdownService _shutdownService = serviceProvider.GetRequiredService<IApplicationShutdownService>();

    protected override void Shutdown()
    {
        _shutdownService.Shutdown(RestartConstants.RestartRequiredCode);
    }
}