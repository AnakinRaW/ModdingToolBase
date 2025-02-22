﻿using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

internal class ManifestDownloader : IManifestDownloader
{
    private readonly IFileSystem _fileSystem;
    private readonly IDownloadManager _downloadManager;
    private string? _temporaryDownloadDirectory;

    private string TemporaryDownloadDirectory
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_temporaryDownloadDirectory) || !_fileSystem.Directory.Exists(_temporaryDownloadDirectory))
                _temporaryDownloadDirectory = _fileSystem.CreateTemporaryFolderInTempWithRetry(10)?.FullName ??
                                              throw new IOException("Unable to create temporary directory");
            return _temporaryDownloadDirectory;
        }
    }

    public ManifestDownloader(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        var config = serviceProvider.GetService<IUpdateConfigurationProvider>()?.GetConfiguration() ??
                     UpdateConfiguration.Default;
        _downloadManager = new DownloadManager(config.DownloadConfiguration, serviceProvider);
    }

    public async Task<IFileInfo> GetManifest(Uri manifestPath, CancellationToken token = default)
    {
        var destPath = CreateRandomFile();
        using var manifest = _fileSystem.FileStream.New(destPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        await _downloadManager.DownloadAsync(manifestPath, manifest, null , null, token);
        return _fileSystem.FileInfo.New(destPath);
    }


    private string CreateRandomFile()
    {
        var location = TemporaryDownloadDirectory;
        string file;
        var count = 0;
        do
        {
            var fileName = _fileSystem.Path.GetRandomFileName();
            file = _fileSystem.Path.Combine(location, fileName);
        } while (_fileSystem.File.Exists(file) && count++ <= 10);
        if (count > 10)
            throw new IOException($"Unable to create temporary file under '{location}'");
        return file;
    }
}