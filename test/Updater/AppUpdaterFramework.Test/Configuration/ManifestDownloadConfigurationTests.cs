using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Configuration;

public class ManifestDownloadConfigurationTests
{
    [Fact]
    public void ToDownloadManagerConfiguration_DefaultBehavior()
    {
        var dmc = new ManifestDownloadConfiguration().ToDownloadManagerConfiguration();
        Assert.Equal(ValidationPolicy.Required, dmc.ValidationPolicy);
        Assert.False(dmc.AllowEmptyFileDownload);
    }

    [Fact]
    public void ToDownloadManagerConfiguration_ForwardsRetryDelayAndClient()
    {
        var dmc = new ManifestDownloadConfiguration
        {
            DownloadRetryDelay = 1234,
            InternetClient = InternetClient.HttpClient
        }.ToDownloadManagerConfiguration();

        Assert.Equal(1234, dmc.DownloadRetryDelay);
        Assert.Equal(InternetClient.HttpClient, dmc.InternetClient);
    }
}
