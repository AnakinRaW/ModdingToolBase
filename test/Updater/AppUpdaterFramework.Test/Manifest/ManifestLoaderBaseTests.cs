using System;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Manifest;

public class ManifestLoaderBaseTests
{
    [Fact]
    public void VerifyAndParse_NullStream_Throws()
    {
        var loader = BuildLoader(SignaturePolicy.Required, hasSignature: true);
        Assert.Throws<ArgumentNullException>(() => loader.LoadAndVerifyManifest(null!));
    }

    [Fact]
    public void VerifyAndParse_PolicyOff_NoSignature_ReturnsManifest()
    {
        var loader = BuildLoader(SignaturePolicy.Off, hasSignature: false);
        using var stream = new MemoryStream([1]);

        var manifest = loader.LoadAndVerifyManifest(stream);
        Assert.NotNull(manifest);
    }

    [Fact]
    public void VerifyAndParse_PolicyOff_MalformedSignatureBlock_Throws()
    {
        var loader = BuildLoader(SignaturePolicy.Off, hasSignature: false, parseResult: VerificationResult.MalformedSignatureBlock);
        using var stream = new MemoryStream([1]);

        var ex = Assert.Throws<SignatureVerificationFailedException>(() => loader.LoadAndVerifyManifest(stream));
        Assert.Equal(VerificationResult.MalformedSignatureBlock, ex.Result);
    }

    [Fact]
    public void VerifyAndParse_PolicyRequired_NoSignature_ThrowsMissingSignature()
    {
        var loader = BuildLoader(SignaturePolicy.Required, hasSignature: false);
        using var stream = new MemoryStream([1]);

        var ex = Assert.Throws<SignatureVerificationFailedException>(() => loader.LoadAndVerifyManifest(stream));
        Assert.Equal(VerificationResult.MissingSignature, ex.Result);
    }

    [Fact]
    public void VerifyAndParse_PolicyRequired_SignaturePresent_VerifierOk_ReturnsManifest()
    {
        var sigVerifier = new StubSigVerifier(VerificationResult.Ok);
        var loader = BuildLoader(SignaturePolicy.Required, hasSignature: true, sigVerifier: sigVerifier);
        using var stream = new MemoryStream([1]);

        var manifest = loader.LoadAndVerifyManifest(stream);
        Assert.NotNull(manifest);
        Assert.Equal(1, sigVerifier.CallCount);
    }

    [Theory]
    [InlineData(VerificationResult.SignatureInvalid)]
    [InlineData(VerificationResult.UntrustedCert)]
    [InlineData(VerificationResult.UnsupportedAlgorithm)]
    public void VerifyAndParse_PolicyRequired_VerifierFails_Throws(VerificationResult sigResult)
    {
        var loader = BuildLoader(SignaturePolicy.Required, hasSignature: true,
            sigVerifier: new StubSigVerifier(sigResult));
        using var stream = new MemoryStream([1]);

        var ex = Assert.Throws<SignatureVerificationFailedException>(() => loader.LoadAndVerifyManifest(stream));
        Assert.Equal(sigResult, ex.Result);
    }

    [Fact]
    public void VerifyAndParse_PolicyOff_DoesNotCallSignatureVerifier()
    {
        var sigVerifier = new StubSigVerifier(VerificationResult.SignatureInvalid);
        var loader = BuildLoader(SignaturePolicy.Off, hasSignature: true, sigVerifier: sigVerifier);
        using var stream = new MemoryStream([1]);

        var manifest = loader.LoadAndVerifyManifest(stream);
        Assert.NotNull(manifest);
        Assert.Equal(0, sigVerifier.CallCount);
    }

    private static StubLoader BuildLoader(
        SignaturePolicy policy,
        bool hasSignature,
        VerificationResult parseResult = VerificationResult.Ok,
        StubSigVerifier? sigVerifier = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(new RealFileSystem());
        services.AddSingleton<IHashingService>(sp => new HashingService(sp));
        services.AddSingleton<ICertificateStore>(sp => new CertificateStore(sp));
        services.AddSingleton<ISignatureVerifier>(sigVerifier ?? new StubSigVerifier(VerificationResult.Ok));
        services.AddSingleton<IUpdateConfigurationProvider>(new StaticConfigProvider(policy));
        return new StubLoader(services.BuildServiceProvider(), parseResult, hasSignature);
    }

    private sealed class StubLoader(IServiceProvider sp, VerificationResult parseResult, bool hasSignature)
        : ManifestLoaderBase(sp)
    {
        protected override ProductManifest ParseManifestAndSignature(
            Stream manifestStream,
            out ParsedSignature? signature)
        {
            if (parseResult != VerificationResult.Ok)
                throw new SignatureVerificationFailedException(parseResult);
            signature = hasSignature
                ? new ParsedSignature("ES256", [1], [2], [3])
                : null;
            return new ProductManifest(new ProductReference("test", null, null), []);
        }
    }

    private sealed class StubSigVerifier(VerificationResult result) : ISignatureVerifier
    {
        public int CallCount { get; private set; }
        public VerificationResult Verify(ParsedSignature parsed)
        {
            CallCount++;
            return result;
        }
    }

    private sealed class StaticConfigProvider(SignaturePolicy policy) : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration() => new()
        {
            DownloadLocation = Path.GetTempPath(),
            ManifestSigningConfiguration = new SigningConfiguration { Policy = policy }
        };
    }
}
