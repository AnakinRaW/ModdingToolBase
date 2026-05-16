using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.CommonUtilities;

namespace AnakinRaW.AppUpdaterFramework.Product;

public abstract class BranchManagerBase : IBranchManager
{
    public abstract string StableBranchName { get; }

    public ProductBranch GetBranchFromName(string branchName)
    {
        ThrowHelper.ThrowIfNullOrEmpty(branchName);
        var isDefault = ProductBranch.BranchNamEqualityComparer.Equals(branchName, StableBranchName);
        var manifestLocations = BuildManifestLocations(branchName);
        return new ProductBranch(branchName, manifestLocations, isDefault);
    }

    public abstract Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync();

    protected abstract IEnumerable<Uri> BuildManifestLocations(string branchName);
}
