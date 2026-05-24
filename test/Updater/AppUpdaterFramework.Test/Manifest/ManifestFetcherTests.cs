using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Testably.Abstractions;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Manifest;

public class ManifestFetcherTests
{
    [Fact]
    public async Task FetchAsync_FirstLocationSucceeds_ReturnsImmediately()
    {
        var (fetcher, loader, pending) = Build([SequencedLoader.Outcome.Ok], _ => Task.CompletedTask);
        var reference = MakeReference(2);

        var manifest = await fetcher.FetchAsync(reference, TestContext.Current.CancellationToken);

        Assert.NotNull(manifest);
        Assert.Equal(1, loader.CallCount);
        Assert.Equal(1, fetcher.DownloadCallCount);
        Assert.NotNull(pending.FetchedManifestBytes);
    }

    [Fact]
    public async Task FetchAsync_SignatureFailureOnFirst_TriesNextMirror()
    {
        var (fetcher, loader, pending) = Build(
            [SequencedLoader.Outcome.SignatureFail, SequencedLoader.Outcome.Ok],
            _ => Task.CompletedTask);
        var reference = MakeReference(2);

        var manifest = await fetcher.FetchAsync(reference, TestContext.Current.CancellationToken);

        Assert.NotNull(manifest);
        Assert.Equal(2, loader.CallCount);
        Assert.Equal(2, fetcher.DownloadCallCount);
        Assert.NotNull(pending.FetchedManifestBytes);
    }

    [Fact]
    public async Task FetchAsync_AllMirrorsFailSignature_SignatureVerificationFailedException()
    {
        var (fetcher, loader, pending) = Build(
            [SequencedLoader.Outcome.SignatureFail, SequencedLoader.Outcome.SignatureFail],
            _ => Task.CompletedTask);
        var reference = MakeReference(2);

        var ex = await Assert.ThrowsAsync<ManifestDownloadException>(
            async () => await fetcher.FetchAsync(reference, TestContext.Current.CancellationToken));
        Assert.IsType<SignatureVerificationFailedException>(ex.InnerException);
        Assert.Equal(2, loader.CallCount);
        Assert.Null(pending.FetchedManifestBytes);
    }

    [Fact]
    public async Task FetchAsync_DownloadFails_TriesNextMirror()
    {
        var attempt = 0;
        var (fetcher, loader, _) = Build(
            [SequencedLoader.Outcome.Ok],
            _ => attempt++ == 0
                ? throw new IOException("simulated download failure")
                : Task.CompletedTask);
        var reference = MakeReference(2);

        var manifest = await fetcher.FetchAsync(reference, TestContext.Current.CancellationToken);
        Assert.NotNull(manifest);
        Assert.Equal(1, loader.CallCount);
    }

    [Fact]
    public async Task FetchAsync_NoLocations_Throws()
    {
        var (fetcher, _, _) = Build([], _ => Task.CompletedTask);
        var reference = MakeReference(0);

        await Assert.ThrowsAsync<ManifestDownloadException>(() => fetcher.FetchAsync(reference, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FetchAsync_OnSuccess_LandsBytesAndBranchInPendingStore()
    {
        var (fetcher, _, pending) = Build(
            [SequencedLoader.Outcome.Ok],
            _ => Task.CompletedTask,
            manifestBranch: "stable");
        var reference = MakeReference(1);

        await fetcher.FetchAsync(reference, TestContext.Current.CancellationToken);

        Assert.NotNull(pending.FetchedManifestBytes);
        Assert.NotEmpty(pending.FetchedManifestBytes!);
        Assert.Equal("stable", pending.FetchedBranch);
    }

    [Fact]
    public void Ctor_PolicyRequiredAndComponentValidationNotRequired_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(new RealFileSystem());
        services.AddSingleton<IHashingService>(sp => new HashingService(sp));
        services.AddSingleton<ICertificateStore>(sp => new CertificateStore(sp));
        services.AddSingleton<ISignatureVerifier>(new AlwaysOkSigVerifier());
        services.AddSingleton<IUpdateConfigurationProvider>(new InconsistentConfigProvider());
        services.AddSingleton<IManifestLoaderProvider>(sp => new ManifestLoaderProvider(new SequencedLoader(sp, [])));
        services.AddSingleton<IPendingUpdateService>(sp => new PendingUpdateService(sp));
        var sp = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => new ManifestFetcher(sp));
        Assert.Contains("ComponentDownloadConfiguration.ValidationPolicy", ex.Message);
    }

    private sealed class InconsistentConfigProvider : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration() => new()
        {
            DownloadLocation = Path.GetTempPath(),
            ManifestSigningConfiguration = new SigningConfiguration { Policy = SignaturePolicy.Required },
            ComponentDownloadConfiguration = new DownloadManagerConfiguration
            {
                ValidationPolicy = ValidationPolicy.NoValidation
            }
        };
    }

    private static (TestFetcher fetcher, SequencedLoader loader, IPendingUpdateService pending) Build(
        IReadOnlyList<SequencedLoader.Outcome> outcomes,
        Func<Uri, Task> downloadBehavior,
        string? manifestBranch = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(new RealFileSystem());
        services.AddSingleton<IHashingService>(sp => new HashingService(sp));
        services.AddSingleton<ICertificateStore>(sp => new CertificateStore(sp));
        services.AddSingleton<ISignatureVerifier>(new AlwaysOkSigVerifier());
        services.AddSingleton<IUpdateConfigurationProvider>(new ConfigProvider());
        SequencedLoader? createdLoader;
        services.AddSingleton<IManifestLoaderProvider>(sp =>
        {
            createdLoader = new SequencedLoader(sp, outcomes, manifestBranch);
            return new ManifestLoaderProvider(createdLoader);
        });
        services.AddSingleton<IPendingUpdateService>(sp => new PendingUpdateService(sp));
        var sp = services.BuildServiceProvider();
        var fetcher = new TestFetcher(sp, downloadBehavior);
        var loader = (SequencedLoader)sp.GetRequiredService<IManifestLoaderProvider>().Loader;
        var pending = sp.GetRequiredService<IPendingUpdateService>();
        return (fetcher, loader, pending);
    }

    private static ProductReference MakeReference(int locationCount)
    {
        var locations = new List<Uri>();
        for (var i = 0; i < locationCount; i++)
            locations.Add(new Uri($"https://example.test/manifest-{i}.json"));
        var branch = new ProductBranch("stable", locations, true);
        return new ProductReference("TestApp", null, branch);
    }

    private sealed class TestFetcher(IServiceProvider sp, Func<Uri, Task> downloadBehavior) : ManifestFetcher(sp)
    {
        public int DownloadCallCount { get; private set; }

        protected override Task DownloadCoreAsync(Uri uri, Stream destination, CancellationToken cancellationToken)
        {
            DownloadCallCount++;
            destination.WriteByte(0x00);
            return downloadBehavior(uri);
        }
    }

    private sealed class AlwaysOkSigVerifier : ISignatureVerifier
    {
        public VerificationResult Verify(ParsedSignature parsed) => VerificationResult.Ok;
    }

    private sealed class SequencedLoader(IServiceProvider sp, IReadOnlyList<SequencedLoader.Outcome> outcomes, string? branch = null)
        : ManifestLoaderBase(sp)
    {
        public enum Outcome { Ok, SignatureFail }
        public int CallCount { get; private set; }

        protected override ProductManifest ParseManifestAndSignature(
            Stream manifestStream,
            out ParsedSignature? signature)
        {
            var i = CallCount++;
            var outcome = outcomes[i];
            signature = outcome switch
            {
                Outcome.Ok => new ParsedSignature("ES256", [1], [2], [3]),
                Outcome.SignatureFail => null,
                _ => throw new InvalidOperationException()
            };
            var productBranch = branch is null ? null : new ProductBranch(branch, [], true);
            return new ProductManifest(new ProductReference("TestApp", null, productBranch), []);
        }
    }

    private sealed class ConfigProvider : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration() => new()
        {
            DownloadLocation = Path.GetTempPath(),
            ManifestSigningConfiguration = new SigningConfiguration { Policy = SignaturePolicy.Required },
            ComponentDownloadConfiguration = new DownloadManagerConfiguration
            {
                ValidationPolicy = ValidationPolicy.Required
            }
        };
    }
}
