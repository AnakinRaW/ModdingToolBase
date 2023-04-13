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
    private readonly Uri _appRootUri;

    private Url BranchLookupUrl => _appRootUri.AppendPathSegment(ApplicationConstants.BranchLookupFileName);

    public ApplicationBranchUtilities(Uri appRootUri)
    {
        _appRootUri = appRootUri;
    }

    public async Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
    {
        var branchesData = await new HttpClient().GetByteArrayAsync(BranchLookupUrl.ToUri());
        var branchNames = Encoding.UTF8.GetString(branchesData).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (!branchNames.Any())
            return Enumerable.Empty<ProductBranch>();

        var branches = new List<ProductBranch>();
        foreach (var name in branchNames)
        {
            var isPrerelease = !name.Equals(ApplicationConstants.StableBranchName, StringComparison.InvariantCultureIgnoreCase);
            branches.Add(new ProductBranch(name, BuildManifestUri(name), isPrerelease));
        }
        return branches;
    }

    public Uri BuildManifestUri(string branchName)
    {
        return _appRootUri.AppendPathSegments(branchName, ApplicationConstants.ManifestFileName).ToUri();
    }

    public Uri BuildComponentUri(string branchName, string fileName)
    {
        return _appRootUri.AppendPathSegments(branchName, fileName).ToUri();
    }
}