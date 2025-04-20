using System.IO;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;

namespace AnakinRaW.AppUpdaterFramework.Configuration;

public class UpdateConfiguration
{
    internal static readonly UpdateConfiguration Default = new()
    {
        DownloadRetryCount = 3,
        DownloadLocation = PathNormalizer.Normalize(Path.GetTempPath(), PathNormalizeOptions.TrimTrailingSeparators),
        BackupLocation = PathNormalizer.Normalize(Path.GetTempPath(), PathNormalizeOptions.TrimTrailingSeparators),
        BackupPolicy = BackupPolicy.NotRequired,
        DownloadConfiguration = DownloadManagerConfiguration.Default
    };

    public byte DownloadRetryCount { get; init; }

    public required string DownloadLocation { get; init; }

    public BackupPolicy BackupPolicy { get; init; }

    public string? BackupLocation { get; init; }

    public bool ValidateInstallation { get; init; }

    public DownloadManagerConfiguration DownloadConfiguration { get; init; } = DownloadManagerConfiguration.Default;

    public UpdateRestartConfiguration RestartConfiguration { get; init; } = new();
}