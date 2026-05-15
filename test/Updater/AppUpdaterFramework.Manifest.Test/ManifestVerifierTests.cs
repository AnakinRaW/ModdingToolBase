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
using ComponentType = AnakinRaW.AppUpdaterFramework.Metadata.Component.ComponentType;

namespace AnakinRaW.AppUpdaterFramework.Manifest.Test;

public class ManifestVerifierTests : TestBaseWithServiceProvider
{
    private readonly IManifestVerifier _verifier;
    private readonly ICertificateStore _trust;

    public ManifestVerifierTests()
    {
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
    public void Verify_NullSignature_ReturnsMissingSignature()
    {
        using var anchor = TestCertificates.CreateSelfSigned();
        _trust.Add(anchor);
        using var stream = ToStream(CreateSample(signature: null));

        Assert.Equal(VerificationResult.MissingSignature, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_EmptyAlgorithm_ReturnsMalformedSignatureBlock()
    {
        using var stream = ToStream(CreateSample(new ManifestSignature(Algorithm: "", Value: "dummy", Certificate: "dummy")));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_EmptySignatureValue_ReturnsMalformedSignatureBlock()
    {
        using var stream = ToStream(CreateSample(new ManifestSignature(Algorithm: "ES256", Value: "", Certificate: "dummy")));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_EmptyCertificate_ReturnsMalformedSignatureBlock()
    {
        using var stream = ToStream(CreateSample(new ManifestSignature(Algorithm: "ES256", Value: "dummy", Certificate: "")));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_UnknownAlgorithm_ReturnsUnsupportedAlgorithm()
    {
        // value/cert are valid base64 so the subclass passes them through to the base, which is
        // where the algorithm check happens.
        var someBase64 = System.Convert.ToBase64String([0x01, 0x02, 0x03]);
        using var stream = ToStream(CreateSample(new ManifestSignature(Algorithm: "PS256", Value: someBase64, Certificate: someBase64)));
        Assert.Equal(VerificationResult.UnsupportedAlgorithm, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_InvalidBase64InValue_ReturnsMalformedSignatureBlock()
    {
        using var stream = ToStream(CreateSample(new ManifestSignature(Algorithm: "ES256", Value: "not-base64!@#", Certificate: "dummy")));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    [Fact]
    public void Verify_MalformedJson_ReturnsMalformedSignatureBlock()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{ not json"));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, _verifier.Verify(stream));
    }

    private static ApplicationManifest CreateSample(ManifestSignature? signature)
    {
        var component = new AppComponent(
            Id: "foo",
            Version: "1.0.0",
            Name: "Foo",
            Type: ComponentType.File,
            Items: null,
            OriginInfo: new OriginInfo("https://example.test/foo.dll", 1, "deadbeef"),
            InstallPath: "$(InstallDir)",
            FileName: "foo.dll",
            InstallSize: new InstallSize(1, 0),
            DetectConditions: null);
        return new ApplicationManifest("TestApp", "1.0.0", "stable", [component]) { Signature = signature };
    }

    private static Stream ToStream(ApplicationManifest manifest)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, manifest, ManifestJsonOptions.Default);
        ms.Position = 0;
        return ms;
    }
}
