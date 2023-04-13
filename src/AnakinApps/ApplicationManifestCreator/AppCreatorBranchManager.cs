using System;
using System.Collections.Generic;
using System.IO.Abstractions;
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
    private readonly ApplicationBranchUtilities _branchUtilities;
    public string StableBranchName => ApplicationConstants.StableBranchName;

    public AppCreatorBranchManager(ManifestCreatorOptions options)
    {
        _branchUtilities = new ApplicationBranchUtilities(options.OriginRootUri);
    }

    public Task<IEnumerable<ProductBranch>> GetAvailableBranches()
    {
        return _branchUtilities.GetAvailableBranchesAsync();
    }

    public Uri GetComponentOrigin(IFileInfo componentFile, ProductBranch branch)
    {
        return _branchUtilities.BuildComponentUri(branch.Name, componentFile.Name);
    }


    public ProductBranch GetBranchFromVersion(SemVersion version)
    {
        var name = BranchManager.GetBranchName(version, StableBranchName, out var isPrerelease);
        return new ProductBranch(name, _branchUtilities.BuildManifestUri(name), isPrerelease);
    }

    public Task<IProductManifest> GetManifest(IProductReference branch, CancellationToken token = default)
    {
        throw new NotSupportedException();
    }
}