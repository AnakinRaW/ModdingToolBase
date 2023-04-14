using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Flurl;

namespace AnakinRaW.ApplicationBase;

public class ApplicationBranchUtilities
{
    public ICollection<Uri> Mirrors { get; }
    
    public ApplicationBranchUtilities(Uri appRootUri) : this(new List<Uri>{appRootUri})
    {
    }

    public ApplicationBranchUtilities(ICollection<Uri> mirrors)
    {
        Mirrors = mirrors;
    }

    public async Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
    {
        var lookupUri = Mirrors.Select(GetBranchLookupUrl);
        var branchesData = await DownloadFromMirrors(lookupUri);
        var branchNames = Encoding.UTF8.GetString(branchesData).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (!branchNames.Any())
            return Enumerable.Empty<ProductBranch>();

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
                return await new HttpClient().GetByteArrayAsync(requestUri);
            }
            catch (HttpRequestException)
            {
                // Ignore and try next mirror
            }
        }
        throw new InvalidOperationException("Unable to download");
    }

    private static Url GetBranchLookupUrl(Uri baseUri)
    {
        return baseUri.AppendPathSegment(ApplicationConstants.BranchLookupFileName);
    }

    public ICollection<Uri> BuildManifestUris(string branchName)
    {
        return Mirrors.Select(mirrorUri => mirrorUri.AppendPathSegments(branchName, ApplicationConstants.ManifestFileName).ToUri()).ToList();
    }

    public static Url BuildComponentUri(Uri baseUri, string branchName, string fileName)
    {
        return baseUri.AppendPathSegments(branchName, fileName);
    }
}