using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IBackupManager : IReadOnlyBackupManager
{
    void BackupComponent(IInstallableComponent component);
    
    void RestoreAll();

    void RemoveBackup(IInstallableComponent component);
}