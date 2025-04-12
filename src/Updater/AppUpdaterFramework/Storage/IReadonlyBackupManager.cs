using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IReadOnlyBackupManager
{
    IDictionary<IInstallableComponent, BackupValueData> Backups { get; }
}