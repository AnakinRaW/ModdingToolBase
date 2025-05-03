using System;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.CommonUtilities.Registry;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

internal class ApplicationUpdateResultHandler(UpdatableApplicationEnvironment applicationEnvironment, IServiceProvider serviceProvider)
    : UpdateResultHandler(serviceProvider)
{
    private readonly IRegistry _registry = serviceProvider.GetRequiredService<IRegistry>();
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override async Task HandleRestart(RestartReason reason)
    {
        using var updaterRegistry = new ApplicationUpdateRegistry(_registry, applicationEnvironment);

        switch (reason)
        {
            case RestartReason.Update:
            {
                var externalUpdaterService = _serviceProvider.GetRequiredService<IExternalUpdaterService>();
                var updateOptions = externalUpdaterService.CreateUpdateOptions();
                var updater = externalUpdaterService.GetExternalUpdater();
                updaterRegistry.ScheduleUpdate(updater, updateOptions);
                break;
            }
            case RestartReason.FailedRestore:
                updaterRegistry.ScheduleReset();
                break;
        }

        await base.HandleRestart(reason);
    }
}