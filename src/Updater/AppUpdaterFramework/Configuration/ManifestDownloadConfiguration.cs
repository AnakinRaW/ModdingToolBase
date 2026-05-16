using AnakinRaW.CommonUtilities.DownloadManager.Configuration;

namespace AnakinRaW.AppUpdaterFramework.Configuration;

/// <summary>
/// Host-configurable subset of <see cref="DownloadManagerConfiguration"/> used for manifest downloads.
/// </summary>
/// <remarks>
/// <see cref="DownloadManagerConfiguration.ValidationPolicy"/> is set to
/// <see cref="ValidationPolicy.Required"/> and <see cref="DownloadManagerConfiguration.AllowEmptyFileDownload"/>
/// is set to <see langword="false"/> for manifest downloads.
/// </remarks>
public sealed class ManifestDownloadConfiguration
{
    /// <summary>
    /// Default configuration: 5 second retry delay, <see cref="InternetClient.HttpClient"/>.
    /// </summary>
    public static readonly ManifestDownloadConfiguration Default = new();

    /// <summary>
    /// Gets the delay in milliseconds before retrying a failed manifest download. Defaults to 5 seconds.
    /// </summary>
    public int DownloadRetryDelay { get; init; } = 5000;

    /// <summary>
    /// Gets the provider used for Internet-based manifest downloads. Defaults to
    /// <see cref="InternetClient.HttpClient"/>.
    /// </summary>
    public InternetClient InternetClient { get; init; } = InternetClient.HttpClient;

    internal DownloadManagerConfiguration ToDownloadManagerConfiguration() => new()
    {
        DownloadRetryDelay = DownloadRetryDelay,
        InternetClient = InternetClient,
        ValidationPolicy = ValidationPolicy.Required,
        AllowEmptyFileDownload = false,
    };
}
