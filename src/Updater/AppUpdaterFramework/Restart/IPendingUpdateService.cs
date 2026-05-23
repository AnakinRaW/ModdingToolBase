using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Restart;

public interface IPendingUpdateService : IPendingUpdateState
{
    /// <summary>
    /// Stores the bytes of an update manifest and the optional branch to the pending update state.
    /// </summary>
    /// <param name="bytes">The bytes of the update manifest.</param>
    /// <param name="branch">The optional branch of the update manifest.</param>
    void SetFetchedManifest(byte[] bytes, string? branch);

    /// <summary>
    /// Adds a pending component to the pending update state.
    /// </summary>
    /// <param name="component">The pending component to add.</param>
    void AddPendingComponent(PendingComponent component);

    /// <summary>
    /// Replaces the pending components in the pending update state with a new collection.
    /// </summary>
    /// <param name="components">The new collection of pending components.</param>
    void ReplacePendingComponents(IEnumerable<PendingComponent> components);

    /// <summary>
    /// Resets the pending update state to its initial empty state.
    /// </summary>
    void Clear();

    /// <summary>
    /// Creates a physical backup for every component in the pending update state.
    /// </summary>
    void BackupPendingComponents();
}
