using System.IO;
using System.Text.Json;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Manifest.Test;

public class ManifestVerifierTests : TestBaseForSigning
{
    private readonly IManifestVerifier _verifier;

    public ManifestVerifierTests()
    {
        _verifier = new JsonManifestVerifier(ServiceProvider);
    }

    [Fact]
    public void Verify_NullSignature_ReturnsMissingSignature()
    {
        using var anchor = TestCertificates.CreateSelfSigned();
        TrustStore.Add(anchor);
        using var stream = ToStream(TestManifests.CreateSample(signature: null));

        Assert.Equal(VerificationResult.MissingSignature, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_EmptyAlgorithm_ReturnsMalformedSignatureBlock()
    {
        using var stream = ToStream(TestManifests.CreateSample(new ManifestSignature(Algorithm: "", Value: "dummy", Certificate: "dummy")));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_EmptySignatureValue_ReturnsMalformedSignatureBlock()
    {
        using var stream = ToStream(TestManifests.CreateSample(new ManifestSignature(Algorithm: "ES256", Value: "", Certificate: "dummy")));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_EmptyCertificate_ReturnsMalformedSignatureBlock()
    {
        using var stream = ToStream(TestManifests.CreateSample(new ManifestSignature(Algorithm: "ES256", Value: "dummy", Certificate: "")));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_UnknownAlgorithm_ReturnsUnsupportedAlgorithm()
    {
        // value/cert are valid base64 so the subclass passes them through to the base, which is
        // where the algorithm check happens.
        var someBase64 = System.Convert.ToBase64String([0x01, 0x02, 0x03]);
        using var stream = ToStream(TestManifests.CreateSample(new ManifestSignature(Algorithm: "PS256", Value: someBase64, Certificate: someBase64)));
        Assert.Equal(VerificationResult.UnsupportedAlgorithm, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_InvalidBase64InValue_ReturnsMalformedSignatureBlock()
    {
        using var stream = ToStream(TestManifests.CreateSample(new ManifestSignature(Algorithm: "ES256", Value: "not-base64!@#", Certificate: "dummy")));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_MalformedJson_ReturnsMalformedSignatureBlock()
    {
        using var stream = new MemoryStream("{ not json"u8.ToArray());
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    private static MemoryStream ToStream(ApplicationManifest manifest)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, manifest, ManifestJsonOptions.Default);
        ms.Position = 0;
        return ms;
    }
}
