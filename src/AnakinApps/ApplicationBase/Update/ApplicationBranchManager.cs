using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Flurl;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

public class ApplicationBranchManager : BranchManager
{
    private readonly IApplicationEnvironment _applicationEnvironment;

    private Url BranchLookupUrl =>
        _applicationEnvironment.UpdateRootUrl.AppendPathSegment(ApplicationConstants.BranchLookupFileName);

    public override string StableBranchName => ApplicationConstants.StableBranchName;

    public ApplicationBranchManager(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _applicationEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();
    }

    public override async Task<IEnumerable<ProductBranch>> GetAvailableBranches()
    {
        var branchesData = await new HttpClient().GetByteArrayAsync(BranchLookupUrl.ToUri());
        var branchNames = Encoding.UTF8.GetString(branchesData).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (!branchNames.Any())
            throw new InvalidOperationException($"No branches detected in '{BranchLookupUrl}'");
        var branches = new List<ProductBranch>();
        foreach (var name in branchNames)
        {
            var isPrerelease = !name.Equals(StableBranchName, StringComparison.InvariantCultureIgnoreCase);
            branches.Add(new ProductBranch(name, BuildManifestUri(name), isPrerelease));
        }
        return branches;
    }

    protected override Uri BuildManifestUri(string branchName)
    {
        return new BranchedUriBuilder(_applicationEnvironment.UpdateRootUrl).BuildManifestUri(branchName);
    }
}