using AnakinRaW.CommonUtilities.DownloadManager.Configuration;

namespace AnakinRaW.ApplicationBase;

public class ApplicationDownloadConfigurationProvider : DownloadManagerConfigurationProviderBase
{
    protected override IDownloadManagerConfiguration CreateConfiguration()
    {
        return new DownloadManagerConfiguration
        {
            AllowEmptyFileDownload = false,
            DownloadRetryDelay = 3,
            InternetClient = InternetClient.HttpClient,
            ValidationPolicy = ValidationPolicy.Optional
        };
    }
}