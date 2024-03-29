using System;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Interaction;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

internal class AppUpdateResultHandler(IServiceProvider serviceProvider) : UpdateResultHandler(serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IApplicationUpdaterRegistry _updaterRegistry = serviceProvider.GetRequiredService<IApplicationUpdaterRegistry>();

    protected override async Task HandleRestart(RestartReason reason)
    {
        switch (reason)
        {
            case RestartReason.Update:
            {
                var updateOptions = _serviceProvider.GetRequiredService<IExternalUpdaterService>().CreateUpdateOptions();
                var updater = _serviceProvider.GetRequiredService<IExternalUpdaterService>().GetExternalUpdater();
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