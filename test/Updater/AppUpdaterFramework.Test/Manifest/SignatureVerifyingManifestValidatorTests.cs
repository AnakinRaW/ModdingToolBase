using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Manifest;

public class SignatureVerifyingManifestValidatorTests
{
    [Fact]
    public async Task ValidateAsync_VerifierReturnsOk_ReturnsTrue()
    {
        // Policy.Off makes the base short-circuit to Ok without crypto involvement.
        var verifier = BuildStub(SignaturePolicy.Off, VerificationResult.Ok);
        var validator = new SignatureVerifyingManifestValidator(verifier);
        using var stream = new MemoryStream([1, 2, 3]);

        Assert.True(await validator.ValidateAsync(stream, stream.Length));
    }

    [Theory]
    [InlineData(VerificationResult.MissingSignature)]
    [InlineData(VerificationResult.MalformedSignatureBlock)]
    public async Task ValidateAsync_VerifierReturnsNonOk_Throws(VerificationResult result)
    {
        var verifier = BuildStub(SignaturePolicy.Required, result);
        var validator = new SignatureVerifyingManifestValidator(verifier);
        using var stream = new MemoryStream([1, 2, 3]);

        var ex = await Assert.ThrowsAsync<SignatureVerificationFailedException>(
            () => validator.ValidateAsync(stream, stream.Length));
        Assert.Equal(result, ex.Result);
    }

    [Fact]
    public async Task ValidateAsync_NullStream_Throws()
    {
        var verifier = BuildStub(SignaturePolicy.Off, VerificationResult.Ok);
        var validator = new SignatureVerifyingManifestValidator(verifier);
        await Assert.ThrowsAsync<ArgumentNullException>(() => validator.ValidateAsync(null!, 0));
    }

    [Fact]
    public async Task ValidateAsync_RewindsSeekableStream()
    {
        var verifier = BuildStub(SignaturePolicy.Required, VerificationResult.MissingSignature);
        var validator = new SignatureVerifyingManifestValidator(verifier);
        using var stream = new MemoryStream([1, 2, 3]);
        stream.Position = stream.Length;

        await Assert.ThrowsAsync<SignatureVerificationFailedException>(
            () => validator.ValidateAsync(stream, stream.Length));
        Assert.Equal(0, verifier.LastObservedPosition);
    }

    [Fact]
    public void Ctor_NullVerifier_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SignatureVerifyingManifestValidator(null!));
    }

    private static StubVerifier BuildStub(SignaturePolicy policy, VerificationResult parseResult)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(new RealFileSystem());
        services.AddSingleton<IHashingService>(sp => new HashingService(sp));
        services.AddSingleton<ICertificateStore>(sp => new CertificateStore(sp));
        services.AddSingleton<ISignatureVerifier>(sp => new SignatureVerifier(sp));
        services.AddSingleton<IUpdateConfigurationProvider>(new StaticConfigProvider(policy));
        return new StubVerifier(services.BuildServiceProvider(), parseResult);
    }

    private sealed class StubVerifier(IServiceProvider sp, VerificationResult parseResult)
        : ManifestVerifierBase(sp)
    {
        public long LastObservedPosition { get; private set; } = -1;

        protected override VerificationResult TryParseSignature(Stream manifestStream, out ParsedSignature? parsed)
        {
            LastObservedPosition = manifestStream.Position;
            parsed = null;
            return parseResult;
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
