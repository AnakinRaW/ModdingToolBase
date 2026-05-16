using System.IO;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Security.Testing;

/// <summary>
/// Common test base for signing-related tests. Wires <see cref="IHashingService"/>,
/// <see cref="ICertificateStore"/>, <see cref="ISignatureVerifier"/>, and an
/// <see cref="IUpdateConfigurationProvider"/> backed by <see cref="CreateSigningConfiguration"/>
/// into the test SP.
/// </summary>
public abstract class TestBaseForSigning : TestBaseWithFileSystem
{
    protected ICertificateStore TrustStore { get; }
    protected SigningConfiguration SigningConfig { get; }

    protected TestBaseForSigning()
    {
        TrustStore = ServiceProvider.GetRequiredService<ICertificateStore>();
        SigningConfig = CreateSigningConfiguration();
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(p => new HashingService(p));
        serviceCollection.AddSingleton<ICertificateStore>(p => new CertificateStore(p));
        serviceCollection.AddSingleton<ISignatureVerifier>(p => new SignatureVerifier(p));
        serviceCollection.AddSingleton<IUpdateConfigurationProvider>(_ => new TestConfigProvider(CreateSigningConfiguration()));
    }

    protected virtual SigningConfiguration CreateSigningConfiguration()
    {
        return SigningConfiguration.Default;
    }

    private sealed class TestConfigProvider(SigningConfiguration signing) : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration() => new()
        {
            DownloadLocation = Path.GetTempPath(),
            ManifestSigningConfiguration = signing,
        };
    }
}
