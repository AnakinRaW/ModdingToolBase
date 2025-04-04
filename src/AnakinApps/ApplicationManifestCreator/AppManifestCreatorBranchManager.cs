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
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using Semver;

namespace AnakinRaW.ApplicationManifestCreator;

internal class AppManifestCreatorBranchManager(ManifestCreatorOptions options, IServiceProvider serviceProvider)
    : IBranchManager
{
    private readonly ApplicationBranchUtilities _branchUtilities = new(options.OriginRootUri, DownloadManagerConfiguration.Default, serviceProvider);
    
    public string StableBranchName => ApplicationConstants.StableBranchName;

    public Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
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

    public Task<IProductManifest> GetManifestAsync(IProductReference branch, CancellationToken token = default)
    {
        throw new NotSupportedException();
    }
}