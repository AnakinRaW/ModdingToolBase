namespace AnakinRaW.ExternalUpdater;

/// <summary>Specifies the outcome of an external updater run as reported back to the hosting application.</summary>
public enum ExternalUpdaterResult
{
    /// <summary>The update failed and the previous state could not be restored from backup.</summary>
    UpdateFailedNoRestore = -2,

    /// <summary>The update failed but the previous state was restored from backup.</summary>
    UpdateFailedWithRestore = -1,

    /// <summary>The external updater was not run.</summary>
    UpdaterNotRun = 0,

    /// <summary>The update completed successfully.</summary>
    UpdateSuccess = 1,

    /// <summary>The application was restarted without performing an update.</summary>
    Restarted = 2
}