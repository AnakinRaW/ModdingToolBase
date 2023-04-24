using System;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

internal class UpdateRestartCommandHandler : UpdateRestartHandler
{
    private readonly IApplicationShutdownService _shutdownService;

    public UpdateRestartCommandHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _shutdownService = serviceProvider.GetRequiredService<IApplicationShutdownService>();
    }

    protected override void Shutdown()
    {
        _shutdownService.Shutdown(0);
    }
}