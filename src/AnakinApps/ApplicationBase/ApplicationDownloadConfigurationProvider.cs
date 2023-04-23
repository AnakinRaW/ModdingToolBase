using AnakinRaW.CommonUtilities.DownloadManager.Configuration;

namespace AnakinRaW.ApplicationBase;

public class ApplicationDownloadConfigurationProvider : DownloadManagerConfigurationProviderBase
{
    protected override IDownloadManagerConfiguration CreateConfiguration()
    {
        return new DownloadManagerConfiguration { VerificationPolicy = VerificationPolicy.Optional };

    }
}