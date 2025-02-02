using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.DownloadManager;
using Flurl;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase;

public class ApplicationBranchUtilities
{
    private readonly IDownloadManager _downloadManager;

    public ICollection<Uri> Mirrors { get; }
    
    public ApplicationBranchUtilities(Uri appRootUri, IServiceProvider serviceProvider) : this(new List<Uri>{appRootUri}, serviceProvider)
    {
    }

    public ApplicationBranchUtilities(ICollection<Uri> mirrors, IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _downloadManager = serviceProvider.GetRequiredService<IDownloadManager>();
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
            var isPrerelease = !name.Equals(ApplicationConstants.StableBranchName, StringComparison.InvariantCultureIgnoreCase);
            branches.Add(new ProductBranch(name, BuildManifestUris(name), isPrerelease));
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
            }
        }

        return [];
    }

    private static Url GetBranchLookupUrl(Uri baseUri)
    {
        return baseUri.AppendPathSegment(ApplicationConstants.BranchLookupFileName);
    }

    public ICollection<Uri> BuildManifestUris(string branchName)
    {
        return Mirrors.Select(mirrorUri => mirrorUri.AppendPathSegments(branchName, ApplicationConstants.ManifestFileName).ToUri()).ToList();
    }

    internal static Url BuildComponentUri(Uri baseUri, string branchName, string fileName)
    {
        return baseUri.AppendPathSegments(branchName, fileName);
    }
}