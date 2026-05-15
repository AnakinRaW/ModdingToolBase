using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.ApplicationManifestCreator.Test;

public class ManifestSignerTests : TestBaseWithServiceProvider
{
    private readonly ManifestSigner _signer;
    private readonly ICertificateStore _trust;
    private readonly IManifestVerifier _verifier;

    public ManifestSignerTests()
    {
        _signer = new ManifestSigner(ServiceProvider);
        _trust = ServiceProvider.GetRequiredService<ICertificateStore>();
        _verifier = new JsonManifestVerifier(ServiceProvider);
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IFileSystem>(new RealFileSystem());
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.AddSingleton(SigningConfiguration.Default);
        serviceCollection.AddSingleton<ICertificateStore>(sp => new CertificateStore(sp));
    }

    [Fact]
    public void Sign_PopulatesSignatureBlock()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var manifest = ManifestFactory.CreateSample();

        var signed = _signer.Sign(manifest, key);

        Assert.NotNull(signed.Signature);
        Assert.False(string.IsNullOrEmpty(signed.Signature!.Value));
        Assert.False(string.IsNullOrEmpty(signed.Signature.Certificate));
    }

    [Fact]
    public void Sign_EmbedsTheSigningCertificateInDerForm()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = _signer.Sign(ManifestFactory.CreateSample(), key);

        var embeddedCertBytes = Convert.FromBase64String(signed.Signature!.Certificate);
        Assert.Equal(key.Certificate.RawData, embeddedCertBytes);
    }

    [Fact]
    public void Sign_Then_Verify_RoundTrips()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = _signer.Sign(ManifestFactory.CreateSample(), key);
        _trust.Add(TestCertificateFactory.PublicCertOf(key));

        using var stream = ToStream(signed);
        Assert.Equal(VerificationResult.Ok, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_TamperedManifest_ReturnsSignatureInvalid()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = _signer.Sign(ManifestFactory.CreateSample(), key);
        _trust.Add(TestCertificateFactory.PublicCertOf(key));

        var tampered = signed with { Version = "9.9.9" };
        using var stream = ToStream(tampered);
        Assert.Equal(VerificationResult.SignatureInvalid, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_CorruptSignatureValue_ReturnsSignatureInvalid()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = _signer.Sign(ManifestFactory.CreateSample(), key);
        _trust.Add(TestCertificateFactory.PublicCertOf(key));

        var originalSig = Convert.FromBase64String(signed.Signature!.Value);
        originalSig[^1] ^= 0xFF;
        var corruptSig = signed.Signature with { Value = Convert.ToBase64String(originalSig) };
        var corrupted = signed with { Signature = corruptSig };

        using var stream = ToStream(corrupted);
        Assert.Equal(VerificationResult.SignatureInvalid, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_SignedByDifferentKey_ReturnsUntrustedCert()
    {
        using var signingKey = TestCertificateFactory.CreateSigningKey();
        using var otherKey = TestCertificateFactory.CreateSigningKey();
        var signed = _signer.Sign(ManifestFactory.CreateSample(), signingKey);
        _trust.Add(TestCertificateFactory.PublicCertOf(otherKey));

        using var stream = ToStream(signed);
        Assert.Equal(VerificationResult.UntrustedCert, _verifier.Verify(stream));
    }

    [Fact]
    public void Sign_WritesConfiguredAlgorithmIntoManifest()
    {
        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = _signer.Sign(ManifestFactory.CreateSample(), key);
        Assert.Equal(nameof(SignatureAlgorithm.ES256), signed.Signature!.Algorithm);
    }

    [Fact]
    public void RoundTrip_WithES384Configuration()
    {
        // Configure the signer with a different algorithm; the verifier reads it back from the
        // manifest itself, so it doesn't need to share the same config.
        var sp = new ServiceCollection()
            .AddSingleton<IFileSystem>(new RealFileSystem())
            .AddSingleton<IHashingService>(p => new HashingService(p))
            .AddSingleton<ICertificateStore>(p => new CertificateStore(p))
            .AddSingleton(new SigningConfiguration { SignatureAlgorithm = SignatureAlgorithm.ES384 })
            .BuildServiceProvider();

        var signer = new ManifestSigner(sp);
        var verifier = new JsonManifestVerifier(sp);
        var trust = sp.GetRequiredService<ICertificateStore>();

        using var key = TestCertificateFactory.CreateSigningKey();
        var signed = signer.Sign(ManifestFactory.CreateSample(), key);
        Assert.Equal(nameof(SignatureAlgorithm.ES384), signed.Signature!.Algorithm);
        trust.Add(TestCertificateFactory.PublicCertOf(key));

        using var stream = ToStream(signed);
        Assert.Equal(VerificationResult.Ok, verifier.Verify(stream));
    }

    private static MemoryStream ToStream(ApplicationManifest manifest)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, manifest, ManifestJsonOptions.Default);
        ms.Position = 0;
        return ms;
    }
}
