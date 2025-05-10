using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using Flurl;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase;

public class ApplicationBranchUtilities
{
    private readonly IDownloadManager _downloadManager;
    private readonly ILogger? _logger;

    public ICollection<Uri> Mirrors { get; }
    
    public ApplicationBranchUtilities(Uri appRootUri, DownloadManagerConfiguration downloadManagerConfiguration, IServiceProvider serviceProvider) 
        : this([appRootUri], downloadManagerConfiguration, serviceProvider)
    {
    }

    public ApplicationBranchUtilities(ICollection<Uri> mirrors, DownloadManagerConfiguration downloadManagerConfiguration, IServiceProvider serviceProvider)
    {
        if (downloadManagerConfiguration == null) 
            throw new ArgumentNullException(nameof(downloadManagerConfiguration));
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _downloadManager = new DownloadManager(downloadManagerConfiguration, serviceProvider);
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        Mirrors = mirrors ?? throw new ArgumentNullException(nameof(mirrors));
    }

    public async Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
    {
        var lookupUri = Mirrors.Select(GetBranchLookupUrl);
        var branchesData = await DownloadFromMirrors(lookupUri);
        var branchNames = Encoding.UTF8.GetString(branchesData).Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        
        if (!branchNames.Any())
            return [];

        var branches = new List<ProductBranch>();
        foreach (var name in branchNames)
        {
            var isDefault = ProductBranch.BranchNamEqualityComparer.Equals(name, ApplicationConstants.StableBranchName);
            branches.Add(new ProductBranch(name, BuildManifestUris(name), isDefault));
        }
        return branches;
    }

    private async Task<byte[]> DownloadFromMirrors(IEnumerable<Url> downloadLocations)
    {
        foreach (var requestUri in downloadLocations)
        {
            try
            {
                using var ms = new MemoryStream();
                await _downloadManager.DownloadAsync(requestUri.ToUri(), ms);
                return ms.ToArray();
            }
            catch (Exception e) when (e is HttpRequestException or DownloadFailedException)
            {
                // Ignore and try next mirror
                _logger?.LogWarning(e, $"Unable to download branch list from mirror '{requestUri}'");
            }
        }

        return [];
    }

    private static Url GetBranchLookupUrl(Uri baseUri)
    {
        return baseUri.AppendPathSegment(ApplicationConstants.BranchLookupFileName);
    }

    public IEnumerable<Uri> BuildManifestUris(string branchName)
    {
        return Mirrors.Select(mirrorUri => mirrorUri.AppendPathSegments(branchName, ApplicationConstants.ManifestFileName).ToUri());
    }

    internal static Url BuildComponentUri(Uri baseUri, string branchName, string fileName)
    {
        return baseUri.AppendPathSegments(branchName, fileName);
    }
}