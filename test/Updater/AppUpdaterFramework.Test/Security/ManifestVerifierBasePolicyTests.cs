using System;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Security;

public class ManifestVerifierBasePolicyTests
{
    [Fact]
    public void Verify_PolicyOff_ShortCircuitsToOkWithoutCallingSubclass()
    {
        var subclass = BuildSubclass(SignaturePolicy.Off);
        using var stream = new MemoryStream([1, 2, 3]);

        Assert.Equal(VerificationResult.Ok, subclass.Verify(stream));
        Assert.Equal(0, subclass.ParseCalls);
    }

    [Fact]
    public void Verify_PolicyRequired_InvokesSubclassParsing()
    {
        var subclass = BuildSubclass(SignaturePolicy.Required);
        subclass.NextResult = VerificationResult.MissingSignature;
        using var stream = new MemoryStream([1, 2, 3]);

        Assert.Equal(VerificationResult.MissingSignature, subclass.Verify(stream));
        Assert.Equal(1, subclass.ParseCalls);
    }

    [Fact]
    public void NullManifestVerifier_PolicyRequired_ReturnsMissingSignature()
    {
        var verifier = new NullManifestVerifier(BuildServices(SignaturePolicy.Required));
        using var stream = new MemoryStream([1, 2, 3]);

        Assert.Equal(VerificationResult.MissingSignature, verifier.Verify(stream));
    }

    [Fact]
    public void NullManifestVerifier_PolicyOff_ReturnsOk()
    {
        var verifier = new NullManifestVerifier(BuildServices(SignaturePolicy.Off));
        using var stream = new MemoryStream([1, 2, 3]);

        Assert.Equal(VerificationResult.Ok, verifier.Verify(stream));
    }

    private static StubVerifier BuildSubclass(SignaturePolicy policy)
        => new(BuildServices(policy));

    private static IServiceProvider BuildServices(SignaturePolicy policy)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(new RealFileSystem());
        services.AddSingleton<IHashingService>(sp => new HashingService(sp));
        services.AddSingleton<ICertificateStore>(sp => new CertificateStore(sp));
        services.AddSingleton<ISignatureVerifier>(sp => new SignatureVerifier(sp));
        services.AddSingleton<IUpdateConfigurationProvider>(new StaticConfigProvider(policy));
        return services.BuildServiceProvider();
    }

    private sealed class StubVerifier(IServiceProvider sp) : ManifestVerifierBase(sp)
    {
        public int ParseCalls { get; private set; }
        public VerificationResult NextResult { get; set; } = VerificationResult.Ok;
        protected override VerificationResult TryParseSignature(Stream manifestStream, out ParsedSignature? parsed)
        {
            ParseCalls++;
            parsed = null;
            return NextResult;
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
