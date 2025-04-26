using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

internal class AppUpdateResultHandler(IServiceProvider serviceProvider) : UpdateResultHandler(serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ApplicationUpdateRegistry _updaterRegistry = serviceProvider.GetRequiredService<ApplicationUpdateRegistry>();

    protected override async Task HandleRestart(RestartReason reason)
    {
        switch (reason)
        {
            case RestartReason.Update:
            {
                var externalUpdaterService = _serviceProvider.GetRequiredService<IExternalUpdaterService>();
                var updateOptions = externalUpdaterService.CreateUpdateOptions();
                var updater = externalUpdaterService.GetExternalUpdater();
                _updaterRegistry.ScheduleUpdate(updater, updateOptions);
                break;
            }
            case RestartReason.FailedRestore:
                _updaterRegistry.ScheduleReset();
                break;
        }

        await base.HandleRestart(reason);
    }
}