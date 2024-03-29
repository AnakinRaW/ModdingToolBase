using System;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.ExternalUpdater;

namespace AnakinRaW.ApplicationBase.Update.External;

public class ExternalUpdaterResultHandler(IApplicationUpdaterRegistry registry)
{
    private readonly IApplicationUpdaterRegistry _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    public void Handle(ExternalUpdaterResult result)
    {
        switch (result)
        {
            case ExternalUpdaterResult.UpdateFailedNoRestore:
                _registry.ScheduleReset();
                break;
            case ExternalUpdaterResult.UpdateFailedWithRestore:
            case ExternalUpdaterResult.UpdateSuccess:
                _registry.Clear();
                break;
            case ExternalUpdaterResult.UpdaterNotRun:
                break;
            case ExternalUpdaterResult.Restarted:
                if (_registry.RequiresUpdate)
                {
                    // Safeguard, since Restarted makes no sense when an update should be performed.
                    // Apparently something went wrong, so we reset the application
                    _registry.ScheduleReset();
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(result), result, null);
        }
    }
}