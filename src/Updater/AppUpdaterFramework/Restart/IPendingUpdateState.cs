using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Restart;

public interface IPendingUpdateState
{
    byte[]? FetchedManifestBytes { get; }

    string? FetchedBranch { get; }

    IReadOnlyCollection<PendingComponent> PendingComponents { get; }
}
