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
    private readonly BranchedUriBuilder _uriBuilder;
    public string StableBranchName => ApplicationConstants.StableBranchName;

    public AppCreatorBranchManager(ManifestCreatorOptions options)
    {
        _uriBuilder = new BranchedUriBuilder(options.OriginRootUri);
    }

    public Task<IEnumerable<ProductBranch>> GetAvailableBranches()
    {
        throw new NotSupportedException();
    }

    public Uri GetComponentOrigin(IFileInfo componentFile, ProductBranch branch)
    {
        return _uriBuilder.BuildComponentUri(branch.Name, componentFile.Name);
    }


    public ProductBranch GetBranchFromVersion(SemVersion version)
    {
        var name = BranchManager.GetBranchName(version, StableBranchName, out var isPrerelease);
        return new ProductBranch(name, _uriBuilder.BuildManifestUri(name), isPrerelease);
    }

    public Task<IProductManifest> GetManifest(IProductReference branch, CancellationToken token = default)
    {
        throw new NotSupportedException();
    }
}