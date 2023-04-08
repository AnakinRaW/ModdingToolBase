using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Restart;

public interface IPendingComponentStore
{
    IReadOnlyCollection<PendingComponent> PendingComponents { get; }
}