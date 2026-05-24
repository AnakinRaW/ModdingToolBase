using System.IO;
using AnakinRaW.AppUpdaterFramework.Security;
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
        ManifestDownloadConfiguration = ManifestDownloadConfiguration.Default,
        BranchDownloadConfiguration = DownloadManagerConfiguration.Default
    };

    public required string DownloadLocation { get; init; }

    public byte DownloadRetryCount { get; init; }

    public DownloadManagerConfiguration ComponentDownloadConfiguration { get; init; } = DownloadManagerConfiguration.Default;

    public ManifestDownloadConfiguration ManifestDownloadConfiguration { get; init; } = ManifestDownloadConfiguration.Default;

    public DownloadManagerConfiguration BranchDownloadConfiguration { get; init; } = DownloadManagerConfiguration.Default;
    
    public bool ValidateInstallation { get; init; }

    public BackupPolicy BackupPolicy { get; init; }

    public string? BackupLocation { get; init; }

    /// <summary>
    /// Gets the directory used to persist a fetched manifest across process restarts so a pending update can be resumed on the next launch.
    /// Defaults to <see cref="DownloadLocation"/> when not explicitly set.
    /// </summary>
    public string PendingUpdateLocation
    {
        get => field ?? DownloadLocation;
        init;
    }

    public UpdateRestartConfiguration RestartConfiguration { get; init; } = new();

    /// <summary>
    /// Signature algorithm and enforcement policy used by the manifest signer and verifier.
    /// </summary>
    public SigningConfiguration ManifestSigningConfiguration { get; init; } = new();
}