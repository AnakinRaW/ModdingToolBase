using System;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Manifest;

public class ManifestFileDownloaderTests : TestBaseWithServiceProvider
{
    private bool _registerVerifier;

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IFileSystem>(new RealFileSystem());
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.AddSingleton<ICertificateStore>(sp => new CertificateStore(sp));
        serviceCollection.AddSingleton<ISignatureVerifier>(sp => new SignatureVerifier(sp));
        serviceCollection.AddSingleton<IUpdateConfigurationProvider>(_ => new ConfigProvider());
        serviceCollection.AddSingleton<ManifestVerifierBase>(sp => _registerVerifier ? new StubVerifier(sp) : null!);
    }

    [Fact]
    public void Ctor_VerifierRegistered_Succeeds()
    {
        _registerVerifier = true;

        using var downloader = new ManifestFileDownloader(ServiceProvider);
        Assert.NotNull(downloader);
    }

    [Fact]
    public void Ctor_NoVerifierRegistered_Throws()
    {
        _registerVerifier = false;

        Assert.Throws<InvalidOperationException>(() => new ManifestFileDownloader(ServiceProvider));
    }

    private sealed class ConfigProvider : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration() => new()
        {
            DownloadLocation = Path.GetTempPath()
        };
    }

    private sealed class StubVerifier(IServiceProvider sp) : ManifestVerifierBase(sp)
    {
        protected override VerificationResult TryParseSignature(Stream manifestStream, out ParsedSignature? parsed)
        {
            parsed = null;
            return VerificationResult.MissingSignature;
        }
    }
}
