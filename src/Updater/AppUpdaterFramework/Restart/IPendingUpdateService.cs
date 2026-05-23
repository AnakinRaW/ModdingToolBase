using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Restart;

public interface IPendingUpdateService : IPendingUpdateState
{
    void SetFetchedManifest(byte[] bytes, string? branch);

    void AddPendingComponent(PendingComponent component);

    /// <summary>
    /// Replaces <see cref="IPendingUpdateState.PendingComponents"/> with the supplied set.
    /// </summary>
    /// <remarks>
    /// Used by the resume-pending-update path to repopulate the in-memory store from a
    /// previously persisted snapshot after the app has re-verified the manifest.
    /// </remarks>
    void ReplacePendingComponents(IEnumerable<PendingComponent> components);

    void Clear();

    /// <summary>
    /// Creates a physical backup for every component currently in <see cref="IPendingUpdateState.PendingComponents"/>.
    /// </summary>
    /// <remarks>
    /// No-op when <see cref="Configuration.UpdateConfiguration.BackupPolicy"/> is <see cref="Configuration.BackupPolicy.Disable"/>.
    /// </remarks>
    void BackupPendingComponents();
}
