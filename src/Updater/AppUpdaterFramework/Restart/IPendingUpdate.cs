namespace AnakinRaW.AppUpdaterFramework.Restart;

public interface IPendingUpdate : IReadOnlyPendingUpdate
{
    /// <summary>
    /// Stores the bytes of an update manifest and the optional branch in the in-memory cache.
    /// </summary>
    void SetFetchedManifest(byte[] bytes, string? branch);

    /// <summary>
    /// Adds a pending component whose installation was deferred until after an application restart.
    /// </summary>
    void AddPendingComponent(PendingComponent component);

    /// <summary>
    /// Resets the pending update to its initial empty state and removes the pending update directory from disk.
    /// </summary>
    void Clear();

    /// <summary>
    /// Persists the currently cached manifest bytes (and branch) to disk so the next process launch can resume the update.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a manifest was written;
    /// <see langword="false"/> if there were no bytes to persist or persistence is not configured.
    /// </returns>
    bool PersistForResume();

    /// <summary>
    /// Loads a previously persisted manifest from disk into the in-memory cache.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if a manifest was loaded;
    /// <see langword="false"/> if nothing was loaded.
    /// </returns>
    bool TryRestoreManifestFromDisk();
}
