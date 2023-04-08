using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

public interface IReadonlyBackupManager
{
    IDictionary<IInstallableComponent, BackupValueData> Backups { get; }
}