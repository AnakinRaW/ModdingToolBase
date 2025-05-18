using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.DownloadManager.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;

namespace AnakinRaW.ApplicationManifestCreator;

internal class AppManifestCreatorBranchManager(ManifestCreatorOptions options, IServiceProvider serviceProvider)
    : IBranchManager
{
    private readonly ApplicationBranchUtilities _branchUtilities = new(options.OriginRootUri, DownloadManagerConfiguration.Default, serviceProvider);
    
    public string StableBranchName => ApplicationConstants.StableBranchName;

    public ProductBranch GetBranchFromName(string branchName)
    {
        ThrowHelper.ThrowIfNullOrEmpty(branchName);
        var isDefault = ProductBranch.BranchNamEqualityComparer.Equals(branchName, StableBranchName);
        return new ProductBranch(branchName, _branchUtilities.BuildManifestUris(branchName), isDefault);
    }

    public Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
    {
        return _branchUtilities.GetAvailableBranchesAsync();
    }

    public Uri GetComponentOrigin(IFileInfo componentFile, ProductBranch branch)
    {
        return ApplicationBranchUtilities.BuildComponentUri(_branchUtilities.Mirrors.First(), branch.Name, componentFile.Name).ToUri();
    }

    public Task<ProductManifest> GetManifestAsync(ProductReference branch, CancellationToken token = default)
    {
        throw new NotSupportedException();
    }
}