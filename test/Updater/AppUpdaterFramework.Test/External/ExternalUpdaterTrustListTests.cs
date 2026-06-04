using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.External;

public class ExternalUpdaterTrustListTests : TestBaseWithFileSystem
{
    // Bytes of the legitimately downloaded + installed on-disk updater.
    private static readonly byte[] OnDiskUpdaterBytes = [0x4D, 0x5A, 0x90, 0x00, 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02];

    // Hash of the updater the running app shipped with — deliberately different from the on-disk one.
    private static readonly byte[] EmbeddedAnchorHash = Enumerable.Repeat((byte)0xAA, 32).ToArray();

    private readonly IHashingService _hashing;
    private readonly string _tempDir;

    // Read lazily by the config provider so each test can pick a signing policy against the single
    // service provider the base builds.
    private SignaturePolicy _policy = SignaturePolicy.Required;

    public ExternalUpdaterTrustListTests()
    {
        _hashing = ServiceProvider.GetRequiredService<IHashingService>();
        _tempDir = FileSystem.Path.Combine(FileSystem.Path.GetTempPath(), $"trust-{Guid.NewGuid():N}");
        FileSystem.Directory.CreateDirectory(_tempDir);
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.AddSingleton<ISignatureVerifier>(new AlwaysOkSigVerifier());
        serviceCollection.AddSingleton<IUpdateConfigurationProvider>(new ConfigProvider(() => _policy));
    }

    // The production regression: updater installed eagerly (absent from pending), bytes differ from the
    // embedded copy, but the signed manifest declares its hash — must be accepted.
    [Fact]
    public void EagerlyInstalledUpdater_DeclaredByManifest_NotInPending_IsAccepted()
    {
        var onDiskHash = Hash(OnDiskUpdaterBytes);

        var trustList = BuildTrustList(
            embedded: new ComponentIntegrityInformation(EmbeddedAnchorHash, HashTypeKey.SHA256),
            fetchedManifest: ManifestWithUpdater(onDiskHash, HashTypeKey.SHA256),
            pending: []);

        var trusted = trustList.GetTrustedIntegrityInformation();

        Assert.Contains(trusted, i => i.HashType == HashTypeKey.SHA256 && i.Hash!.SequenceEqual(onDiskHash));

        var check = new ExternalUpdaterIntegrityCheck(ServiceProvider);
        using var onDisk = WriteAndOpen(OnDiskUpdaterBytes);
        check.EnsureMatchesAny(onDisk, trusted); // must not throw
    }

    // Pre-fix behavior: with no manifest source and nothing pending, only the embedded anchor is
    // trusted, so a byte-different on-disk updater is rejected.
    [Fact]
    public void NoManifestNoPending_OnlyEmbeddedAnchorTrusted_MismatchIsRejected()
    {
        var trustList = BuildTrustList(
            embedded: new ComponentIntegrityInformation(EmbeddedAnchorHash, HashTypeKey.SHA256),
            fetchedManifest: null,
            pending: []);

        var trusted = trustList.GetTrustedIntegrityInformation();

        Assert.Single(trusted);
        Assert.True(trusted.Single().Hash!.SequenceEqual(EmbeddedAnchorHash));

        var check = new ExternalUpdaterIntegrityCheck(ServiceProvider);
        using var onDisk = WriteAndOpen(OnDiskUpdaterBytes);
        Assert.Throws<SecurityException>(() => check.EnsureMatchesAny(onDisk, trusted));
    }

    [Fact]
    public void EmbeddedAnchor_IsAlwaysIncluded()
    {
        var trustList = BuildTrustList(
            embedded: new ComponentIntegrityInformation(EmbeddedAnchorHash, HashTypeKey.SHA256),
            fetchedManifest: ManifestWithUpdater(Hash(OnDiskUpdaterBytes), HashTypeKey.SHA256),
            pending: []);

        var trusted = trustList.GetTrustedIntegrityInformation();
        Assert.Contains(trusted, i => i.Hash!.SequenceEqual(EmbeddedAnchorHash));
    }

    // Deferred (locked) updater: integrity comes from the pending component.
    [Fact]
    public void DeferredUpdater_InPendingComponents_IsAccepted()
    {
        var pendingHash = Enumerable.Repeat((byte)0xCC, 32).ToArray();

        var trustList = BuildTrustList(
            embedded: new ComponentIntegrityInformation(EmbeddedAnchorHash, HashTypeKey.SHA256),
            fetchedManifest: null,
            pending: [PendingUpdaterComponent(pendingHash, HashTypeKey.SHA256)]);

        var trusted = trustList.GetTrustedIntegrityInformation();
        Assert.Contains(trusted, i => i.Hash!.SequenceEqual(pendingHash));
    }

    // Preserved guard: Required policy + pending updater lacking integrity fails loudly.
    [Fact]
    public void RequiredPolicy_PendingUpdaterWithoutIntegrity_Throws()
    {
        var trustList = BuildTrustList(
            embedded: new ComponentIntegrityInformation(EmbeddedAnchorHash, HashTypeKey.SHA256),
            fetchedManifest: null,
            pending: [PendingUpdaterComponent(hash: null, HashTypeKey.None)]);

        var ex = Assert.Throws<InvalidOperationException>(() => trustList.GetTrustedIntegrityInformation());
        Assert.Contains("signing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmbeddedAnchorMissing_Throws()
    {
        var trustList = BuildTrustList(
            embedded: ComponentIntegrityInformation.None,
            fetchedManifest: null,
            pending: []);

        Assert.Throws<InvalidOperationException>(() => trustList.GetTrustedIntegrityInformation());
    }

    // Off policy: an unsigned manifest is still parsed, so its declared hash is trusted.
    [Fact]
    public void OffPolicy_UnsignedManifest_StillTrustsDeclaredHash()
    {
        var onDiskHash = Hash(OnDiskUpdaterBytes);

        var trustList = BuildTrustList(
            embedded: new ComponentIntegrityInformation(EmbeddedAnchorHash, HashTypeKey.SHA256),
            fetchedManifest: ManifestWithUpdater(onDiskHash, HashTypeKey.SHA256),
            pending: [],
            policy: SignaturePolicy.Off,
            provideSignature: false);

        var trusted = trustList.GetTrustedIntegrityInformation();
        Assert.Contains(trusted, i => i.Hash!.SequenceEqual(onDiskHash));
    }

    [Fact]
    public void Ctor_NullArgument_Throws()
    {
        var loader = new StubManifestLoader(ServiceProvider, EmptyManifest());
        var config = ServiceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
        var provider = new FakeUpdaterProvider(new ComponentIntegrityInformation(EmbeddedAnchorHash, HashTypeKey.SHA256));
        var pending = new FakePendingUpdate();

        Assert.Throws<ArgumentNullException>(() => new ExternalUpdaterTrustList(null!, pending, loader, config));
        Assert.Throws<ArgumentNullException>(() => new ExternalUpdaterTrustList(provider, null!, loader, config));
        Assert.Throws<ArgumentNullException>(() => new ExternalUpdaterTrustList(provider, pending, null!, config));
        Assert.Throws<ArgumentNullException>(() => new ExternalUpdaterTrustList(provider, pending, loader, null!));
    }

    // ---- helpers --------------------------------------------------------------------------------

    private byte[] Hash(byte[] data)
    {
        using var stream = new MemoryStream(data, writable: false);
        return _hashing.GetHash(stream, HashTypeKey.SHA256);
    }

    private Stream WriteAndOpen(byte[] bytes)
    {
        var path = FileSystem.Path.Combine(_tempDir, "AnakinRaW.ExternalUpdater.exe");
        FileSystem.File.WriteAllBytes(path, bytes);
        return FileSystem.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private ExternalUpdaterTrustList BuildTrustList(
        ComponentIntegrityInformation embedded,
        ProductManifest? fetchedManifest,
        IReadOnlyCollection<PendingComponent> pending,
        SignaturePolicy policy = SignaturePolicy.Required,
        bool provideSignature = true)
    {
        _policy = policy;
        var loader = new StubManifestLoader(ServiceProvider, fetchedManifest ?? EmptyManifest(), provideSignature);
        var config = ServiceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
        var pendingState = new FakePendingUpdate
        {
            // Stub loader ignores the bytes; any non-empty buffer triggers the parse path.
            FetchedManifestBytes = fetchedManifest is null ? null : [0x01, 0x02, 0x03],
            PendingComponents = pending
        };
        return new ExternalUpdaterTrustList(new FakeUpdaterProvider(embedded), pendingState, loader, config);
    }

    private static ProductManifest ManifestWithUpdater(byte[]? hash, HashTypeKey hashType)
    {
        var origin = new OriginInfo(new Uri("https://example.test/AnakinRaW.ExternalUpdater.exe"))
        {
            Size = 10,
            IntegrityInformation = new ComponentIntegrityInformation(hash, hashType)
        };
        var updater = new SingleFileComponent(
            ExternalUpdaterConstants.ComponentIdentity, null, "[AppData]",
            ExternalUpdaterConstants.GetExecutableFileName(), origin);
        return new ProductManifest(new ProductReference("TestApp"), [updater]);
    }

    private static ProductManifest EmptyManifest()
    {
        return new ProductManifest(new ProductReference("TestApp"), []);
    }

    private static PendingComponent PendingUpdaterComponent(byte[]? hash, HashTypeKey hashType)
    {
        var origin = hash is null
            ? null
            : new OriginInfo(new Uri("https://example.test/AnakinRaW.ExternalUpdater.exe"))
            {
                IntegrityInformation = new ComponentIntegrityInformation(hash, hashType)
            };
        var updater = new SingleFileComponent(
            ExternalUpdaterConstants.ComponentIdentity, null, "[AppData]",
            ExternalUpdaterConstants.GetExecutableFileName(), origin);
        return new PendingComponent { Component = updater, Action = UpdateAction.Update };
    }

    private sealed class FakeUpdaterProvider(ComponentIntegrityInformation integrity) : IExternalUpdaterProvider
    {
        public ComponentIntegrityInformation GetIntegrity()
        {
            return integrity;
        }

        public void EnsureAvailable(bool force = false) { }
    }

    private sealed class FakePendingUpdate : IReadOnlyPendingUpdate
    {
        public byte[]? FetchedManifestBytes { get; set; }
        public string? FetchedBranch { get; set; }
        public IReadOnlyCollection<PendingComponent> PendingComponents { get; set; } = [];
    }

    private sealed class AlwaysOkSigVerifier : ISignatureVerifier
    {
        public VerificationResult Verify(ParsedSignature parsed)
        {
            return VerificationResult.Ok;
        }
    }

    private sealed class StubManifestLoader(IServiceProvider sp, ProductManifest manifest, bool provideSignature = true)
        : ManifestLoaderBase(sp)
    {
        protected override ProductManifest ParseManifestAndSignature(Stream manifestStream, out ParsedSignature? signature)
        {
            signature = provideSignature ? new ParsedSignature("ES256", [1], [2], [3]) : null;
            return manifest;
        }
    }

    private sealed class ConfigProvider(Func<SignaturePolicy> policy) : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration()
        {
            return new UpdateConfiguration
            {
                DownloadLocation = "/updates",
                ManifestSigningConfiguration = new SigningConfiguration { Policy = policy() },
                ComponentDownloadConfiguration = new DownloadManagerConfiguration
                    { ValidationPolicy = ValidationPolicy.Required }
            };
        }
    }
}
