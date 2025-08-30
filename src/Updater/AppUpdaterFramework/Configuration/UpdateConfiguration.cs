using System.IO;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;

namespace AnakinRaW.AppUpdaterFramework.Configuration;

public class UpdateConfiguration
{
    public static readonly UpdateConfiguration Default = new()
    {
        DownloadRetryCount = 3,
        DownloadLocation = PathNormalizer.Normalize(Path.GetTempPath(), PathNormalizeOptions.TrimTrailingSeparators),
        BackupLocation = PathNormalizer.Normalize(Path.GetTempPath(), PathNormalizeOptions.TrimTrailingSeparators),
        BackupPolicy = BackupPolicy.NotRequired,
        ComponentDownloadConfiguration = DownloadManagerConfiguration.Default,
        ManifestDownloadConfiguration = DownloadManagerConfiguration.Default,
        BranchDownloadConfiguration = DownloadManagerConfiguration.Default
    };

    public required string DownloadLocation { get; init; }

    public byte DownloadRetryCount { get; init; }

    public DownloadManagerConfiguration ComponentDownloadConfiguration { get; init; } = DownloadManagerConfiguration.Default;
    
    public DownloadManagerConfiguration ManifestDownloadConfiguration { get; init; } = DownloadManagerConfiguration.Default;
    
    public DownloadManagerConfiguration BranchDownloadConfiguration { get; init; } = DownloadManagerConfiguration.Default;
    
    public bool ValidateInstallation { get; init; }

    public BackupPolicy BackupPolicy { get; init; }

    public string? BackupLocation { get; init; }

    public UpdateRestartConfiguration RestartConfiguration { get; init; } = new();
}