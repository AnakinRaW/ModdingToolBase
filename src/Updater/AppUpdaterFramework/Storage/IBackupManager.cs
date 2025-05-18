using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IBackupManager : IReadOnlyBackupManager
{
    void BackupComponent(InstallableComponent component);
    
    void RestoreAll();

    void RemoveBackup(InstallableComponent component);
}