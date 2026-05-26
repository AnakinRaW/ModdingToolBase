using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Manifest.Test;

public class JsonManifestLoaderTests : TestBaseForSigning
{
    private readonly JsonManifestLoader _loader;
    private SignaturePolicy _policy = SignaturePolicy.Off;
    private VerificationResult _verifierResult = VerificationResult.Ok;

    public JsonManifestLoaderTests()
    {
        _loader = new JsonManifestLoader(ServiceProvider);
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IBranchManager>(_ => new StubBranchManager());
        serviceCollection.AddSingleton<IUpdateConfigurationProvider>(new DynamicConfigProvider(() => _policy));
        serviceCollection.AddSingleton<ISignatureVerifier>(new StubSignatureVerifier(() => _verifierResult));
    }

    [Fact]
    public void LoadAndVerifyManifest_MalformedJson_ThrowsManifestException()
    {
        using var stream = new MemoryStream("{ not json"u8.ToArray());
        Assert.Throws<ManifestException>(() => _loader.LoadAndVerifyManifest(stream));
    }

    [Fact]
    public void LoadAndVerifyManifest_PolicyOff_UnsignedManifest_ReturnsManifest()
    {
        _policy = SignaturePolicy.Off;
        using var stream = ToStream(TestManifests.CreateSample());

        var manifest = _loader.LoadAndVerifyManifest(stream);
        Assert.Equal("TestApp", manifest.Product.Name);
    }

    [Fact]
    public void LoadAndVerifyManifest_PolicyOff_MalformedSignatureBlock_StillThrows()
    {
        _policy = SignaturePolicy.Off;
        using var stream = ToStream(TestManifests.CreateSample(
            new ManifestSignature("ES256", "not-base64!@#", "dummy")));

        var ex = Assert.Throws<SignatureVerificationFailedException>(() => _loader.LoadAndVerifyManifest(stream));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, ex.Result);
    }

    [Fact]
    public void LoadAndVerifyManifest_EmptySignatureFields_ThrowsMalformed()
    {
        using var stream = ToStream(TestManifests.CreateSample(
            new ManifestSignature("", "v", "c")));
        var ex = Assert.Throws<SignatureVerificationFailedException>(() => _loader.LoadAndVerifyManifest(stream));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, ex.Result);
    }

    [Fact]
    public void LoadAndVerifyManifest_PolicyRequired_UnsignedManifest_ThrowsMissingSignature()
    {
        _policy = SignaturePolicy.Required;
        using var stream = ToStream(TestManifests.CreateSample());

        var ex = Assert.Throws<SignatureVerificationFailedException>(() => _loader.LoadAndVerifyManifest(stream));
        Assert.Equal(VerificationResult.MissingSignature, ex.Result);
    }

    [Fact]
    public void LoadAndVerifyManifest_PolicyRequired_VerifierOk_ReturnsManifest()
    {
        _policy = SignaturePolicy.Required;
        _verifierResult = VerificationResult.Ok;
        var someBase64 = Convert.ToBase64String([0x01]);
        using var stream = ToStream(TestManifests.CreateSample(
            new ManifestSignature("ES256", someBase64, someBase64)));

        var manifest = _loader.LoadAndVerifyManifest(stream);
        Assert.Equal("TestApp", manifest.Product.Name);
    }

    [Theory]
    [InlineData(VerificationResult.SignatureInvalid)]
    [InlineData(VerificationResult.UntrustedCert)]
    [InlineData(VerificationResult.UnsupportedAlgorithm)]
    public void LoadAndVerifyManifest_PolicyRequired_VerifierFails_Throws(VerificationResult sigResult)
    {
        _policy = SignaturePolicy.Required;
        _verifierResult = sigResult;
        var someBase64 = Convert.ToBase64String([0x01]);
        using var stream = ToStream(TestManifests.CreateSample(
            new ManifestSignature("ES256", someBase64, someBase64)));

        var ex = Assert.Throws<SignatureVerificationFailedException>(() => _loader.LoadAndVerifyManifest(stream));
        Assert.Equal(sigResult, ex.Result);
    }

    private static MemoryStream ToStream(ApplicationManifest manifest)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, manifest, ManifestJsonOptions.Default);
        ms.Position = 0;
        return ms;
    }

    private sealed class StubSignatureVerifier(Func<VerificationResult> result) : ISignatureVerifier
    {
        public VerificationResult Verify(ParsedSignature parsed) => result();
    }

    private sealed class DynamicConfigProvider(Func<SignaturePolicy> policy) : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration() => new()
        {
            DownloadLocation = Path.GetTempPath(),
            ManifestSigningConfiguration = new SigningConfiguration { Policy = policy() }
        };
    }

    private sealed class StubBranchManager : IBranchManager
    {
        public string StableBranchName => "stable";
        
        public ProductBranch GetBranchFromName(string branchName)
        {
            return new ProductBranch(branchName, [], branchName == StableBranchName);
        }

        public Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
        {
            return Task.FromResult<IEnumerable<ProductBranch>>([]);
        }
    }
}
