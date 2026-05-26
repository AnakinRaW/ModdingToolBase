using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Restart;

public interface IReadOnlyPendingUpdate
{
    /// <summary>
    /// Gets the raw bytes of the fetched manifest, or <see langword="null"/> if none was fetched.
    /// </summary>
    byte[]? FetchedManifestBytes { get; }

    /// <summary>
    /// Gets the branch name of the fetched manifest, or <see langword="null"/> if none was fetched or specified.
    /// </summary>
    string? FetchedBranch { get; }

    /// <summary>
    /// Gets the components whose installation is deferred until after an application restart.
    /// </summary>
    IReadOnlyCollection<PendingComponent> PendingComponents { get; }
}
