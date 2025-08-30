using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;

namespace AnakinRaW.ApplicationBase.Update;

public class ApplicationBranchManager : BranchManagerBase
{
    private readonly ApplicationBranchUtilities _branchUtilities;

    public override string StableBranchName => ApplicationConstants.StableBranchName;

    public ApplicationBranchManager(UpdatableApplicationEnvironment applicationEnvironment, IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
       
        _branchUtilities = new ApplicationBranchUtilities(
            applicationEnvironment.UpdateMirrors,
            applicationEnvironment.UpdateConfiguration.BranchDownloadConfiguration,
            serviceProvider);
    }

    public override Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
    {
        return _branchUtilities.GetAvailableBranchesAsync();
    }

    protected override IEnumerable<Uri> BuildManifestLocations(string branchName)
    {
        return _branchUtilities.BuildManifestUris(branchName);
    }
}