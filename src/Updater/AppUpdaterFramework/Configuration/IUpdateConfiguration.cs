using AnakinRaW.CommonUtilities.DownloadManager.Configuration;

namespace AnakinRaW.AppUpdaterFramework.Configuration;

public interface IUpdateConfiguration
{
    byte DownloadRetryCount { get; }

    string DownloadLocation { get; }

    string? BackupLocation { get; }

    BackupPolicy BackupPolicy { get; }

    bool SupportsRestart { get; }

    bool ValidateInstallation { get; }

    DownloadManagerConfiguration DownloadConfiguration { get; }

    bool PassCurrentArgumentsForRestart { get; }
}