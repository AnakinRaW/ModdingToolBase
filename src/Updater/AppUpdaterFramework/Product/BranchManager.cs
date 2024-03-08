using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Product;

public abstract class BranchManagerBase : IBranchManager
{
    private readonly ILogger? _logger;
    private readonly IManifestDownloader _manifestDownloader;

    protected readonly IManifestLoader ManifestLoader;

    public abstract string StableBranchName { get; }

    protected BranchManagerBase(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _manifestDownloader = serviceProvider.GetService<IManifestDownloader>() ?? new ManifestDownloader(serviceProvider);
        ManifestLoader = serviceProvider.GetRequiredService<IManifestLoader>();
    }

    public abstract Task<IEnumerable<ProductBranch>> GetAvailableBranches();

    public static string GetBranchName(SemVersion version, string defaultName, out bool isPrerelease)
    {
        var branchName = version.Prerelease;
        if (string.IsNullOrEmpty(branchName))
            branchName = defaultName;
        isPrerelease = version.IsPrerelease;
        return branchName;
    }

    public virtual ProductBranch GetBranchFromVersion(SemVersion version)
    {
        var branchName = GetBranchName(version, StableBranchName, out var preRelease);
        var manifestUris = BuildManifestUris(branchName);
        return new ProductBranch(branchName, manifestUris, preRelease);
    }

    public async Task<IProductManifest> GetManifest(IProductReference productReference, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        Requires.NotNull(productReference, nameof(productReference));
        if (productReference.Branch is null)
            throw new InvalidOperationException("No branch specified.");

        var branch = productReference.Branch;

        IFileInfo manifestFile = null!;

        DownloadFailedException? lastException = null;

        foreach (var manifestLocation in branch.ManifestLocations)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                manifestFile = await _manifestDownloader.GetManifest(manifestLocation, token);
                if (manifestFile.Exists && manifestFile.Length != 0)
                    break;
            }
            catch (DownloadFailedException ex)
            {
                _logger?.LogError(ex, ex.Message);
                lastException = ex;
            }
        }

        if (lastException is not null)
            throw new CatalogDownloadException("Could not download branch manifest from all sources.", lastException);
        
        try
        {
            token.ThrowIfCancellationRequested();
            var manifest = await ManifestLoader.LoadManifest(manifestFile, productReference, token);
            return manifest ?? throw new CatalogException("No catalog was created");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"Unable to get manifest of branch:' {branch.Name}'";
            _logger?.LogError(ex, message);
            throw new CatalogException(message, ex);
        }
        finally
        {
            try
            {
                manifestFile.DeleteIfInTemp();
            }
            catch (Exception e)
            {
                _logger?.LogWarning($"Failed to delete manifest {e.Message}");
            }
        }
    }
    
    protected abstract ICollection<Uri> BuildManifestUris(string branchName);
}