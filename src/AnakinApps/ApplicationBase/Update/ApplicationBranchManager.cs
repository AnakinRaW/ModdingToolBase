using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

public class ApplicationBranchManager : BranchManagerBase
{
    private readonly ApplicationBranchUtilities _branchUtilities;

    public override string StableBranchName => ApplicationConstants.StableBranchName;

    public ApplicationBranchManager(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        var applicationEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();
        _branchUtilities = new ApplicationBranchUtilities(
            applicationEnvironment.UpdateMirrors,
            applicationEnvironment.UpdateDownloadManagerConfiguration,
            serviceProvider);
    }

    public override Task<IEnumerable<ProductBranch>> GetAvailableBranches()
    {
        return _branchUtilities.GetAvailableBranchesAsync();
    }

    protected override ICollection<Uri> BuildManifestUris(string branchName)
    {
        return _branchUtilities.BuildManifestUris(branchName);
    }
}