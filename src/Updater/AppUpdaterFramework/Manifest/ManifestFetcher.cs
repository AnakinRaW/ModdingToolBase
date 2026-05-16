using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Manifest;

internal class ManifestFetcher : IManifestFetcher
{
    private readonly IFileSystem _fileSystem;
    private readonly IDownloadManager _downloadManager;
    private readonly ManifestLoaderBase _manifestLoader;
    private readonly ILogger? _logger;

    public ManifestFetcher(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _manifestLoader = serviceProvider.GetRequiredService<IManifestLoaderProvider>().Loader;
        var config = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
        _downloadManager = new DownloadManager(config.ManifestDownloadConfiguration.ToDownloadManagerConfiguration(), serviceProvider);
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<ManifestFetcher>();
    }

    public async Task<ProductManifest> FetchAsync(ProductReference productReference, CancellationToken cancellationToken)
    {
        if (productReference is null) 
            throw new ArgumentNullException(nameof(productReference));

        var branch = productReference.Branch
            ?? throw new InvalidOperationException("No branch specified on product reference.");
        if (branch.ManifestLocations.Count == 0)
            throw new ManifestDownloadException("No location to an update manifest specified.");

        Exception? lastException = null;
        IDirectoryInfo? tempDir = null;
        try
        {
            tempDir = _fileSystem.CreateTemporaryFolderInTempWithRetry(10);
            Debug.Assert(tempDir is not null);
            foreach (var manifestUri in branch.ManifestLocations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var manifest = await DownloadAndVerifyAsync(manifestUri, tempDir!, cancellationToken).ConfigureAwait(false);
                    ValidateCompatibleManifest(manifest.Product, productReference);
                    return manifest;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Manifest fetch from '{Uri}' failed: {Message}", manifestUri, ex.Message);
                    lastException = ex;
                }
            }
        }
        finally
        {
            TryDelete(tempDir);
        }

        var message = $"Unable to get manifest of branch: '{branch.Name}'";
        if (lastException is not null)
            message += $". Last error: {lastException.Message}";
        _logger?.LogError(lastException, "{Message}", message);
        throw new ManifestDownloadException(message, lastException);
    }
    
    protected virtual Task DownloadCoreAsync(Uri uri, Stream destination, CancellationToken cancellationToken)
        => _downloadManager.DownloadAsync(uri, destination, null, null, null, cancellationToken);

    private async Task<ProductManifest> DownloadAndVerifyAsync(Uri manifestUri, IDirectoryInfo tempDir, CancellationToken token)
    {
        var destPath = CreateRandomFile(tempDir);
#if NETSTANDARD2_1_OR_GREATER || NET
        await
#endif
        using var fileStream = _fileSystem.FileStream.New(destPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        _logger?.LogDebug("Downloading manifest from '{Source}' to '{Destination}'.", manifestUri.AbsolutePath, destPath);
        await DownloadCoreAsync(manifestUri, fileStream, token).ConfigureAwait(false);
        fileStream.Position = 0;
        return _manifestLoader.LoadAndVerifyManifest(fileStream);
    }

    private string CreateRandomFile(IDirectoryInfo tempDir)
    {
        var location = tempDir.FullName;
        string file;
        var count = 0;
        do
        {
            var fileName = _fileSystem.Path.GetRandomFileName();
            file = _fileSystem.Path.Combine(location, fileName);
        } 
        while (_fileSystem.File.Exists(file) && count++ <= 10);

        return count > 10 
            ? throw new IOException($"Unable to create temporary in '{location}'") 
            : file;
    }

    private static void TryDelete(IDirectoryInfo? dir)
    {
        if (dir is null) 
            return;
        try
        {
            dir.Delete(true);
        }
        catch
        {
            // Ignore
        }
    }

    private static void ValidateCompatibleManifest(ProductReference manifestProduct, ProductReference installedProduct)
    {
        if (!ProductReferenceEqualityComparer.NameOnly.Equals(manifestProduct, installedProduct))
            throw new ManifestException(
                $"Manifest for '{manifestProduct.Name}' does not match installed product '{installedProduct.Name}' by name.");
    }
}
