using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Semver;

namespace AnakinRaW.ApplicationManifestCreator;

internal class AppCreatorBranchManager : IBranchManager
{
    public string StableBranchName => ApplicationConstants.StableBranchName;

    public Task<IEnumerable<ProductBranch>> GetAvailableBranches()
    {
        throw new NotSupportedException();
    }

    public ProductBranch GetBranchFromVersion(SemVersion version)
    {
        var name = BranchManager.GetBranchName(version, StableBranchName, out var isPrerelease);
        var builder = new BranchUriBuilder(new Uri(""));
        return new ProductBranch(name, builder.Build(name), isPrerelease);
    }

    public Task<IProductManifest> GetManifest(IProductReference branch, CancellationToken token = default)
    {
        throw new NotSupportedException();
    }
}