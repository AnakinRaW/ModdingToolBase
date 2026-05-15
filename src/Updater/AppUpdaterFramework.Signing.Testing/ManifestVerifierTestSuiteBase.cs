using System;
using System.IO;
using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Security.Testing;

/// <summary>
/// Abstract xunit test suite for <see cref="ManifestVerifierBase"/> implementations.
/// </summary>
/// <remarks>
/// Subclass this and implement the four hooks (verifier factory + the three stream factories) to
/// get a baseline of verifier tests for free: null-stream guard, missing-signature handling,
/// untrusted-cert handling, tampered-content handling, and a full sign-verify round-trip across
/// every <see cref="SignatureAlgorithm"/>.
/// </remarks>
public abstract class ManifestVerifierTestSuiteBase : TestBaseWithServiceProvider
{
    protected IManifestVerifier Verifier { get; }
    protected ICertificateStore TrustStore { get; }

    protected ManifestVerifierTestSuiteBase()
    {
        TrustStore = ServiceProvider.GetRequiredService<ICertificateStore>();
        Verifier = CreateVerifier(ServiceProvider);
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IFileSystem>(new RealFileSystem());
        serviceCollection.AddSingleton<IHashingService>(p => new HashingService(p));
        serviceCollection.AddSingleton(SigningConfiguration.Default);
        serviceCollection.AddSingleton<ICertificateStore>(p => new CertificateStore(p));
    }

    /// <summary>Creates the verifier instance under test.</summary>
    protected abstract IManifestVerifier CreateVerifier(IServiceProvider serviceProvider);

    /// <summary>Produces a stream containing a syntactically valid manifest with no signature block.</summary>
    protected abstract Stream CreateUnsignedManifestStream();

    /// <summary>
    /// Produces a stream containing a manifest signed with <paramref name="signingKey"/>, embedded
    /// with <paramref name="signingCert"/> and the given <paramref name="algorithm"/>.
    /// </summary>
    protected abstract Stream CreateSignedManifestStream(
        ECDsa signingKey,
        X509Certificate2 signingCert,
        SignatureAlgorithm algorithm);

    /// <summary>
    /// Produces a stream containing a manifest signed by <paramref name="signingKey"/> but with
    /// content modified after signing so the signature no longer verifies.
    /// </summary>
    protected abstract Stream CreateTamperedSignedManifestStream(
        ECDsa signingKey,
        X509Certificate2 signingCert,
        SignatureAlgorithm algorithm);

    [Fact]
    public void Verify_NullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Verifier.Verify(null!));
    }

    [Fact]
    public void Verify_UnsignedManifest_ReturnsMissingSignature()
    {
        using var stream = CreateUnsignedManifestStream();
        Assert.Equal(VerificationResult.MissingSignature, Verifier.Verify(stream));
    }

    [Theory]
    [InlineData(SignatureAlgorithm.ES256)]
    [InlineData(SignatureAlgorithm.ES384)]
    [InlineData(SignatureAlgorithm.ES512)]
    public void Verify_RoundTrip_Succeeds(SignatureAlgorithm algorithm)
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair(algorithm);
        using (key)
        using (cert)
        using (var stream = CreateSignedManifestStream(key, cert, algorithm))
        {
            TrustStore.Add(cert);
            Assert.Equal(VerificationResult.Ok, Verifier.Verify(stream));
        }
    }

    [Fact]
    public void Verify_UntrustedCert_ReturnsUntrustedCert()
    {
        var (signKey, signCert) = TestCertificates.CreateEcdsaSigningPair();
        using (signKey)
        using (signCert)
        using (var stream = CreateSignedManifestStream(signKey, signCert, SignatureAlgorithm.ES256))
        {
            // Trust store intentionally empty.
            Assert.Equal(VerificationResult.UntrustedCert, Verifier.Verify(stream));
        }
    }

    [Fact]
    public void Verify_TamperedSignedManifest_ReturnsSignatureInvalid()
    {
        var (key, cert) = TestCertificates.CreateEcdsaSigningPair();
        using (key)
        using (cert)
        using (var stream = CreateTamperedSignedManifestStream(key, cert, SignatureAlgorithm.ES256))
        {
            TrustStore.Add(cert);
            Assert.Equal(VerificationResult.SignatureInvalid, Verifier.Verify(stream));
        }
    }
}
