using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

public sealed class ManifestFileDownloader : DisposableObject
{
    private readonly IFileSystem _fileSystem;
    private readonly IDownloadManager _downloadManager;

    private readonly IDirectoryInfo? _tempDirectory;
    
    public ManifestFileDownloader(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        var config = serviceProvider.GetService<IUpdateConfigurationProvider>()?.GetConfiguration() ?? 
                     UpdateConfiguration.Default;
        _downloadManager = new DownloadManager(config.DownloadConfiguration, serviceProvider);


        _tempDirectory = _fileSystem.CreateTemporaryFolderInTempWithRetry(10);
    }

    public async Task<IFileInfo> DownloadManifest(Uri manifestPath, CancellationToken token = default)
    {
        var destPath = CreateRandomFile();
#if NETSTANDARD2_1_OR_GREATER || NET
        await
#endif
        using var manifest = _fileSystem.FileStream.New(destPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        await _downloadManager.DownloadAsync(manifestPath, manifest, null , null, token);
        return _fileSystem.FileInfo.New(destPath);
    }

    protected override void DisposeResources()
    {
        base.DisposeResources();
        try
        {
            _tempDirectory?.Delete(true);
        }
        catch
        {
            // Ignore
        }
    }

    private string CreateRandomFile()
    {
        var location = _tempDirectory?.FullName ?? _fileSystem.Path.GetTempPath();
        string file;
        var count = 0;
        do
        {
            var fileName = _fileSystem.Path.GetRandomFileName();
            file = _fileSystem.Path.Combine(location, fileName);
        } while (_fileSystem.File.Exists(file) && count++ <= 10);
        if (count > 10)
            throw new IOException($"Unable to create temporary in '{location}'");
        return file;
    }
}