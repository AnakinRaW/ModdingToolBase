using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            applicationEnvironment.UpdateConfiguration.DownloadConfiguration,
            serviceProvider);
    }

    public override Task<IEnumerable<ProductBranch>> GetAvailableBranchesAsync()
    {
        return _branchUtilities.GetAvailableBranchesAsync();
    }

    protected override ICollection<Uri> BuildManifestUris(string branchName)
    {
        return _branchUtilities.BuildManifestUris(branchName);
    }
}