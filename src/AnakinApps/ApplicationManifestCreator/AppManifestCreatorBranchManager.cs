using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Semver;

namespace AnakinRaW.ApplicationManifestCreator;

internal class AppManifestCreatorBranchManager(ManifestCreatorOptions options, IServiceProvider serviceProvider)
    : IBranchManager
{
    private readonly ApplicationBranchUtilities _branchUtilities = new ApplicationBranchUtilities(options.OriginRootUri, serviceProvider);
    public string StableBranchName => ApplicationConstants.StableBranchName;

    public Task<IEnumerable<ProductBranch>> GetAvailableBranches()
    {
        return _branchUtilities.GetAvailableBranchesAsync();
    }

    public Uri GetComponentOrigin(IFileInfo componentFile, ProductBranch branch)
    {
        return ApplicationBranchUtilities.BuildComponentUri(_branchUtilities.Mirrors.First(), branch.Name, componentFile.Name).ToUri();
    }


    public ProductBranch GetBranchFromVersion(SemVersion version)
    {
        var name = BranchManagerBase.GetBranchName(version, StableBranchName, out var isPrerelease);
        return new ProductBranch(name, _branchUtilities.BuildManifestUris(name), isPrerelease);
    }

    public Task<IProductManifest> GetManifest(IProductReference branch, CancellationToken token = default)
    {
        throw new NotSupportedException();
    }
}