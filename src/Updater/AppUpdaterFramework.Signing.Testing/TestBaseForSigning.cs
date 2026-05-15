using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Security.Testing;

/// <summary>
/// Common test base for signing-related tests. Wires <see cref="IHashingService"/> and
/// <see cref="ICertificateStore"/> into the test SP, and exposes the trust store and an active
/// <see cref="SigningConfiguration"/> as typed properties.
/// </summary>
/// <remarks>
/// <see cref="SigningConfiguration"/> is intentionally not registered in DI — it lives inside
/// <c>UpdateConfiguration</c> for runtime use. Tests that need to vary the configuration override
/// <see cref="CreateSigningConfiguration"/>.
/// </remarks>
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
    }

    protected virtual SigningConfiguration CreateSigningConfiguration()
    {
        return SigningConfiguration.Default;
    }
}
