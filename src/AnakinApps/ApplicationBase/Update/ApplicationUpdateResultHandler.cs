using System;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Restart;
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
                ScheduleDeferredUpdate(updaterRegistry);
                break;
            }
            case RestartReason.RestoreFailed:
                Logger?.LogTrace("Scheduling registry reset.");
                updaterRegistry.ScheduleReset();
                break;
        }

        base.RestartApplication(reason);
    }

    private void ScheduleDeferredUpdate(ApplicationUpdateRegistry updaterRegistry)
    {
        var pending = _serviceProvider.GetRequiredService<IPendingUpdate>();
        var branch = pending.FetchedBranch;

        if (string.IsNullOrEmpty(branch))
        {
            Logger?.LogError("Fetched manifest has no branch name; cannot schedule a deferred update. Falling back to a registry reset to recover from a possibly dirty install.");
            updaterRegistry.ScheduleReset();
            return;
        }

        if (!pending.PersistForResume())
        {
            Logger?.LogError("Failed to persist pending-update manifest. Falling back to a registry reset to recover from a possibly dirty install.");
            updaterRegistry.ScheduleReset();
            return;
        }

        Logger?.LogTrace("Scheduling deferred update on branch '{Branch}'.", branch);
        updaterRegistry.UpdateBranch = branch;
        updaterRegistry.RequiresUpdate = true;
    }
}
