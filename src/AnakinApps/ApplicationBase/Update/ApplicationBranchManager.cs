using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace AnakinRaW.ApplicationBase.Update;

public class ApplicationBranchManager : BranchManager
{
    private readonly ApplicationBranchUtilities _branchUtilities;

    public override string StableBranchName => ApplicationConstants.StableBranchName;

    public ApplicationBranchManager(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        var applicationEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();
        _branchUtilities = new ApplicationBranchUtilities(applicationEnvironment.UpdateRootUrl);
    }

    public override Task<IEnumerable<ProductBranch>> GetAvailableBranches()
    {
        return _branchUtilities.GetAvailableBranchesAsync();
    }

    protected override Uri BuildManifestUri(string branchName)
    {
        return _branchUtilities.BuildManifestUri(branchName);
    }
}