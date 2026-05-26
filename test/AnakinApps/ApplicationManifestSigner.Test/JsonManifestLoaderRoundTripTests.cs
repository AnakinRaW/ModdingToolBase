using System;
using System.IO;
using System.Text.Json;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.ApplicationManifestSigner.Test;

public class JsonManifestLoaderRoundTripTests : TestBaseForSigning
{
    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IBranchManager>(_ => new StubBranchManager());
    }

    [Theory]
    [InlineData(SignatureAlgorithm.ES256)]
    [InlineData(SignatureAlgorithm.ES384)]
    [InlineData(SignatureAlgorithm.ES512)]
    public void VerifyAndParse_RoundTrip_ReturnsManifest(SignatureAlgorithm algorithm)
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair(algorithm);
        using (key)
        using (cert)
        {
            TrustStore.Add(cert);
            var signed = Sign(TestManifests.CreateSample(), key, cert, algorithm);

            using var stream = ToStream(signed);
            var loader = new JsonManifestLoader(ServiceProvider);
            var manifest = loader.LoadAndVerifyManifest(stream);
            Assert.Equal("TestApp", manifest.Product.Name);
        }
    }

    [Fact]
    public void VerifyAndParse_UnsignedUnderPolicyRequired_ThrowsMissingSignature()
    {
        using var stream = ToStream(TestManifests.CreateSample());
        var loader = new JsonManifestLoader(ServiceProvider);
        var ex = Assert.Throws<SignatureVerificationFailedException>(() => loader.LoadAndVerifyManifest(stream));
        Assert.Equal(VerificationResult.MissingSignature, ex.Result);
    }

    [Fact]
    public void VerifyAndParse_UntrustedCert_ThrowsUntrustedCert()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        {
            // Trust store intentionally empty.
            var signed = Sign(TestManifests.CreateSample(), key, cert, SignatureAlgorithm.ES256);
            using var stream = ToStream(signed);
            var loader = new JsonManifestLoader(ServiceProvider);
            var ex = Assert.Throws<SignatureVerificationFailedException>(() => loader.LoadAndVerifyManifest(stream));
            Assert.Equal(VerificationResult.UntrustedCert, ex.Result);
        }
    }

    [Fact]
    public void VerifyAndParse_TamperedContent_ThrowsSignatureInvalid()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        {
            TrustStore.Add(cert);
            var signed = Sign(TestManifests.CreateSample(), key, cert, SignatureAlgorithm.ES256);
            var tampered = signed with { Version = "9.9.9" };
            using var stream = ToStream(tampered);

            var loader = new JsonManifestLoader(ServiceProvider);
            var ex = Assert.Throws<SignatureVerificationFailedException>(() => loader.LoadAndVerifyManifest(stream));
            Assert.Equal(VerificationResult.SignatureInvalid, ex.Result);
        }
    }

    [Fact]
    public void VerifyAndParse_CorruptedSignatureBytes_ThrowsSignatureInvalid()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        {
            TrustStore.Add(cert);
            var signed = Sign(TestManifests.CreateSample(), key, cert, SignatureAlgorithm.ES256);
            var sigBytes = Convert.FromBase64String(signed.Signature!.Value);
            sigBytes[^1] ^= 0xFF;
            var corrupted = signed with
            {
                Signature = signed.Signature with { Value = Convert.ToBase64String(sigBytes) }
            };
            using var stream = ToStream(corrupted);

            var loader = new JsonManifestLoader(ServiceProvider);
            var ex = Assert.Throws<SignatureVerificationFailedException>(() => loader.LoadAndVerifyManifest(stream));
            Assert.Equal(VerificationResult.SignatureInvalid, ex.Result);
        }
    }

    [Fact]
    public void VerifyAndParse_PolicyOff_TamperedManifest_StillReturns()
    {
        // Reconstruct a service provider with Policy=Off, then assert tampered manifests pass.
        var sigCfgSnapshot = new SigningConfiguration { Policy = SignaturePolicy.Off };
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        {
            var signed = Sign(TestManifests.CreateSample(), key, cert, SignatureAlgorithm.ES256);
            var tampered = signed with { Version = "9.9.9" };
            using var stream = ToStream(tampered);

            var sp = BuildServicesWithPolicy(sigCfgSnapshot);
            var loader = new JsonManifestLoader(sp);
            var manifest = loader.LoadAndVerifyManifest(stream);
            Assert.Equal("TestApp", manifest.Product.Name);
        }
    }

    private IServiceProvider BuildServicesWithPolicy(SigningConfiguration cfg)
    {
        var services = new ServiceCollection();
        services.AddSingleton<System.IO.Abstractions.IFileSystem>(new Testably.Abstractions.RealFileSystem());
        services.AddSingleton<IHashingService>(sp => new HashingService(sp));
        services.AddSingleton<ICertificateStore>(sp => new CertificateStore(sp));
        services.AddSingleton<ISignatureVerifier>(sp => new SignatureVerifier(sp));
        services.AddSingleton<IUpdateConfigurationProvider>(new StaticConfigProvider(cfg));
        services.AddSingleton<IBranchManager>(_ => new StubBranchManager());
        return services.BuildServiceProvider();
    }

    private ApplicationManifest Sign(ApplicationManifest manifest, System.Security.Cryptography.ECDsa key,
        System.Security.Cryptography.X509Certificates.X509Certificate2 cert, SignatureAlgorithm algorithm)
    {
        var signer = new ManifestSigner(
            ServiceProvider.GetRequiredService<IHashingService>(),
            new SigningConfiguration { SignatureAlgorithm = algorithm });
        return signer.Sign(manifest, new SigningKey(key, cert));
    }

    private static Stream ToStream(ApplicationManifest manifest)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, manifest, ManifestJsonOptions.Default);
        ms.Position = 0;
        return ms;
    }

    private sealed class StaticConfigProvider(SigningConfiguration cfg) : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration() => new()
        {
            DownloadLocation = Path.GetTempPath(),
            ManifestSigningConfiguration = cfg
        };
    }

    private sealed class StubBranchManager : IBranchManager
    {
        public string StableBranchName => "stable";
        public ProductBranch GetBranchFromName(string branchName)
            => new(branchName, [], branchName == StableBranchName);
        public System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
            => System.Threading.Tasks.Task.FromResult<System.Collections.Generic.IEnumerable<ProductBranch>>([]);
        public System.Threading.Tasks.Task<AppUpdaterFramework.Metadata.Manifest.ProductManifest> GetManifestAsync(
            ProductReference branch, System.Threading.CancellationToken token = default)
            => throw new NotSupportedException();
    }
}
