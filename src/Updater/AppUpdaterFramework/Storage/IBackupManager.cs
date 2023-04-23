using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IBackupManager : IReadonlyBackupManager
{
    void BackupComponent(IInstallableComponent component);

    void RestoreBackup(IInstallableComponent component);

    void RestoreAll();

    void RemoveBackups();

    void RemoveBackup(IInstallableComponent component);
}