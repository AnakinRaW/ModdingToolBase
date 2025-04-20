using System;
using System.Threading;

namespace AnakinRaW.AppUpdaterFramework.Configuration;

public abstract class UpdateConfigurationProviderBase : IUpdateConfigurationProvider
{
    private UpdateConfiguration _lazyConfiguration = null!;

    public UpdateConfiguration GetConfiguration()
    {
        var configuration = LazyInitializer.EnsureInitialized(ref _lazyConfiguration, CreateConfiguration);
        if (configuration is null)
            throw new InvalidOperationException("Configuration must not be null");
        return configuration;
    }

    protected abstract UpdateConfiguration CreateConfiguration();
}