using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.DownloadManager.Providers;
using AnakinRaW.CommonUtilities.DownloadManager.Validation;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnakinRaW.AppUpdaterFramework.Test.Updater.Tasks;

public class DownloadStepTests : TestBaseWithFileSystem, IDisposable
{
    private const string DownloadDir = "/download-dir";
    private readonly IHashingService _hashing;

    public DownloadStepTests()
    { 
        FileSystem.Directory.CreateDirectory(DownloadDir); 
        _hashing = ServiceProvider.GetRequiredService<IHashingService>();
    }

    protected override void SetupServices(IServiceCollection serviceCollection)
    {
        base.SetupServices(serviceCollection);
        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.AddSingleton<IUpdateConfigurationProvider>(new TestConfigProvider(DownloadDir));
        serviceCollection.AddSingleton<IDownloadRepositoryFactory>(sp => new DownloadRepositoryFactory(sp));
    }

    public void Dispose()
    {
        try
        {
            FileSystem.Directory.Delete(DownloadDir, recursive: true);
        }
        catch
        {
            // Ignore
        }
    }

    [Fact]
    public async Task CacheHit_SkipsDownload()
    {
        var content = "the-cached-payload"u8.ToArray();
        var component = ComponentFor(content);
        var dm = new RecordingDownloadManager(content);

        var stagedPath = StageDeterministicFile(component, content);

        var step = new DownloadStep(component, TestConfig(), dm, EmptyVars(), ServiceProvider);
        await step.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, dm.DownloadCallCount);
        Assert.Equal(content, FileSystem.File.ReadAllBytes(stagedPath));
        Assert.Equal(stagedPath, step.DownloadPath);
    }

    [Fact]
    public async Task CachedFileSizeMismatch_Downloads()
    {
        var declared = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var component = ComponentFor(declared);
        var dm = new RecordingDownloadManager(declared);

        StageDeterministicFile(component, [9, 9]);

        var step = new DownloadStep(component, TestConfig(), dm, EmptyVars(), ServiceProvider);
        await step.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, dm.DownloadCallCount);
        Assert.Equal(declared, FileSystem.File.ReadAllBytes(step.DownloadPath));
    }

    [Fact]
    public async Task CachedFileHashMismatch_Downloads()
    {
        var declared = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var component = ComponentFor(declared);
        var dm = new RecordingDownloadManager(declared);

        var staged = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 };
        StageDeterministicFile(component, staged);

        var step = new DownloadStep(component, TestConfig(), dm, EmptyVars(), ServiceProvider);
        await step.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, dm.DownloadCallCount);
        Assert.Equal(declared, FileSystem.File.ReadAllBytes(step.DownloadPath));
    }

    [Fact]
    public async Task NoIntegrityDeclared_Downloads()
    {
        var content = "some-bytes"u8.ToArray();
        var component = ComponentWithoutIntegrity(content.LongLength);
        var dm = new RecordingDownloadManager(content);

        var step = new DownloadStep(component, TestConfig(), dm, EmptyVars(), ServiceProvider);
        await step.RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, dm.DownloadCallCount);
    }

    private SingleFileComponent ComponentFor(byte[] content)
    {
        var hash = _hashing.GetHash(new MemoryStream(content), HashTypeKey.SHA256);
        var origin = new OriginInfo(new Uri("https://example.test/file.bin"))
        {
            Size = content.LongLength,
            IntegrityInformation = new ComponentIntegrityInformation(hash, HashTypeKey.SHA256),
        };
        return new SingleFileComponent("comp", null, DownloadDir, "file.bin", origin);
    }

    private SingleFileComponent ComponentWithoutIntegrity(long size)
    {
        var origin = new OriginInfo(new Uri("https://example.test/file.bin")) { Size = size };
        return new SingleFileComponent("comp", null, DownloadDir, "file.bin", origin);
    }

    private string StageDeterministicFile(SingleFileComponent component, byte[] bytes)
    {
        var integrity = component.OriginInfo!.IntegrityInformation;
        var hashHex = ToHex(integrity.Hash!);
        var fileName = $"{component.FileName}.{hashHex}.new";
        var path = FileSystem.Path.Combine(DownloadDir, fileName);
        FileSystem.File.WriteAllBytes(path, bytes);
        return FileSystem.FileInfo.New(path).FullName;
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) 
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static UpdateConfiguration TestConfig()
    {
        return new UpdateConfiguration
        {
            DownloadLocation = DownloadDir,
            DownloadRetryCount = 0,
        };
    }

    private static IReadOnlyDictionary<string, string> EmptyVars()
    {
        return new Dictionary<string, string>();
    }

    private sealed class TestConfigProvider(string downloadDir) : IUpdateConfigurationProvider
    {
        public UpdateConfiguration GetConfiguration()
        {
            return new UpdateConfiguration
            {
                DownloadLocation = downloadDir,
                DownloadRetryCount = 0,
            };
        }
    }

    private sealed class RecordingDownloadManager(byte[] payload) : IDownloadManager
    {
        public int DownloadCallCount { get; private set; }

        public IEnumerable<string> Providers => [];

        public void AddDownloadProvider(IDownloadProvider provider)
        {
            throw new NotSupportedException();
        }

        public async Task<DownloadResult> DownloadAsync(
            Uri uri,
            Stream destination,
            DownloadUpdateCallback? progress,
            DownloadOptions? options,
            IDownloadValidator? validator,
            CancellationToken cancellationToken)
        {
            DownloadCallCount++;
            await destination.WriteAsync(payload, 0, payload.Length, cancellationToken);
            return new DownloadResult(uri);
        }
    }
}
