using System;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.CommonUtilities.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Update;

public abstract class ApplicationUpdateResultHandler(UpdatableApplicationEnvironment applicationEnvironment, IServiceProvider serviceProvider)
    : UpdateResultHandler(serviceProvider)
{
    private readonly IRegistry _registry = serviceProvider.GetRequiredService<IRegistry>();
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override void RestartApplication(RestartReason reason)
    {
        using var updaterRegistry = new ApplicationUpdateRegistry(_registry, applicationEnvironment);

        switch (reason)
        {
            case RestartReason.Update:
            {
                var externalUpdaterService = _serviceProvider.GetRequiredService<IExternalUpdaterService>();
                var updateOptions = externalUpdaterService.CreateUpdateOptions();
                var updater = externalUpdaterService.GetExternalUpdater(); 
                Logger?.LogTrace("Scheduling update in registry.");
                updaterRegistry.ScheduleUpdate(updater, updateOptions);
                break;
            }
            case RestartReason.RestoreFailed:
                Logger?.LogTrace("Scheduling registry reset.");
                updaterRegistry.ScheduleReset();
                break;
        }

        base.RestartApplication(reason);
    }
}