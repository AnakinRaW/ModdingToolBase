using System.IO.Abstractions;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.AppUpdaterFramework.External;

public interface IExternalUpdaterService
{
    ExternalUpdateOptions CreateUpdateOptions();

    ExternalRestartOptions CreateRestartOptions(bool elevate = false);

    IFileInfo GetExternalUpdater();

    void Launch(ExternalUpdaterOptions options);

    /// <summary>
    /// Creates a backup for every component currently in <see cref="Restart.IPendingComponentStore"/>.
    /// </summary>
    /// <remarks>
    /// No-op when <see cref="Configuration.UpdateConfiguration.BackupPolicy"/> is
    /// <see cref="Configuration.BackupPolicy.Disable"/>.
    /// </remarks>
    void BackupPendingComponents();
}