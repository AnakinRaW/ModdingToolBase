using System;
using System.Collections.Generic;
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
        var pendingState = _serviceProvider.GetRequiredService<IPendingUpdateState>();

        var manifestBytes = pendingState.FetchedManifestBytes;
        if (manifestBytes is null)
        {
            Logger?.LogError("No fetched manifest bytes available; cannot schedule a deferred update. Falling back to a registry reset to recover from a possibly dirty install.");
            updaterRegistry.ScheduleReset();
            return;
        }

        var branch = pendingState.FetchedBranch;
        if (string.IsNullOrEmpty(branch))
        {
            Logger?.LogError("Fetched manifest has no branch name; cannot schedule a deferred update. Falling back to a registry reset to recover from a possibly dirty install.");
            updaterRegistry.ScheduleReset();
            return;
        }

        var components = new List<PendingComponentRef>();
        foreach (var pending in pendingState.PendingComponents)
        {
            components.Add(new PendingComponentRef
            {
                ComponentId = pending.Component.Id,
                Action = pending.Action,
            });
        }

        Logger?.LogTrace("Scheduling deferred update with {Count} pending components on branch '{Branch}'.",
            components.Count, branch);

        try
        {
            new PendingUpdateStore(applicationEnvironment, _serviceProvider).Save(new PendingUpdateSnapshot
            {
                ManifestBytes = manifestBytes,
                Components = components,
                Branch = branch!,
            });
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Failed to persist pending-update snapshot: {Message}. Falling back to a registry reset to recover from a possibly dirty install.", ex.Message);
            updaterRegistry.ScheduleReset();
            return;
        }

        updaterRegistry.UpdateBranch = branch;
        updaterRegistry.RequiresUpdate = true;
    }
}
