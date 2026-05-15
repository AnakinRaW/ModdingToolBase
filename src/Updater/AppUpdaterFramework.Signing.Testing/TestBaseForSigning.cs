using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Security.Testing;

public abstract class TestBaseForSigning : TestBaseWithFileSystem
{ 
    protected ICertificateStore TrustStore { get; }

    protected TestBaseForSigning()
    {
        TrustStore = ServiceProvider.GetRequiredService<ICertificateStore>();
    }
    
    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(p => new HashingService(p));
        serviceCollection.AddSingleton(CreateSigningConfiguration());
        serviceCollection.AddSingleton<ICertificateStore>(p => new CertificateStore(p));
    }

    protected virtual SigningConfiguration CreateSigningConfiguration()
    {
        return SigningConfiguration.Default;
    }
}