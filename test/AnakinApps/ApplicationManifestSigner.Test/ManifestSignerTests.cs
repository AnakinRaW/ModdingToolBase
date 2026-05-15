using System;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.ApplicationManifestSigner.Test;

/// <summary>
/// Signer-specific assertions. Sign+verify round-trips across the supported algorithms are covered
/// by <see cref="JsonManifestVerifierSuite"/> (via the inherited <c>ManifestVerifierTestSuiteBase</c>).
/// </summary>
public class ManifestSignerTests : TestBaseForSigning
{
    private readonly ManifestSigner _signer;

    public ManifestSignerTests()
    {
        _signer = new ManifestSigner(ServiceProvider);
    }

    [Fact]
    public void Sign_PopulatesSignatureBlock()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var manifest = TestManifests.CreateSample();

        var signed = _signer.Sign(manifest, key);

        Assert.NotNull(signed.Signature);
        Assert.False(string.IsNullOrEmpty(signed.Signature!.Value));
        Assert.False(string.IsNullOrEmpty(signed.Signature.Certificate));
    }

    [Fact]
    public void Sign_EmbedsTheSigningCertificateInDerForm()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = _signer.Sign(TestManifests.CreateSample(), key);

        var embeddedCertBytes = Convert.FromBase64String(signed.Signature!.Certificate);
        Assert.Equal(key.Certificate.RawData, embeddedCertBytes);
    }

    [Fact]
    public void Sign_WritesConfiguredAlgorithmIntoManifest()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = _signer.Sign(TestManifests.CreateSample(), key);
        Assert.Equal(nameof(SignatureAlgorithm.ES256), signed.Signature!.Algorithm);
    }

    [Fact]
    public void Sign_WritesConfiguredAlgorithmIntoManifest_ES384()
    {
        var sp = new ServiceCollection()
            .AddSingleton<IFileSystem>(new RealFileSystem())
            .AddSingleton<IHashingService>(p => new HashingService(p))
            .AddSingleton(new SigningConfiguration { SignatureAlgorithm = SignatureAlgorithm.ES384 })
            .BuildServiceProvider();
        var signer = new ManifestSigner(sp);

        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = signer.Sign(TestManifests.CreateSample(), key);
        Assert.Equal(nameof(SignatureAlgorithm.ES384), signed.Signature!.Algorithm);
    }
}
