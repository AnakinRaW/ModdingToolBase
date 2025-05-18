using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Updater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.CommonUtilities.Registry;

namespace AnakinRaW.ApplicationBase.Update;

public abstract class ApplicationUpdater
{
    protected readonly UpdatableApplicationEnvironment Environment;
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IProductService ProductService;
    protected readonly IBranchManager BranchManager;
    protected readonly IUpdateService UpdateService;
    protected readonly ILogger? Logger;

    protected ApplicationUpdater(UpdatableApplicationEnvironment environment, IServiceProvider serviceProvider)
    {
        Environment = environment;
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        ProductService = ServiceProvider.GetRequiredService<IProductService>();
        BranchManager = ServiceProvider.GetRequiredService<IBranchManager>();
        UpdateService = ServiceProvider.GetRequiredService<IUpdateService>();
        Logger = ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public string GetBranchNameFromRegistry(string? branchName, bool setRegistry)
    {
        var registry = ServiceProvider.GetRequiredService<IRegistry>();
        using var updateRegistry = new ApplicationUpdateRegistry(registry, Environment);

        var stableBranchName = BranchManager.StableBranchName;

        if (!string.IsNullOrEmpty(branchName))
        {
            if (setRegistry)
            {
                updateRegistry.SetBranch(ProductBranch.BranchNamEqualityComparer.Equals(branchName!, stableBranchName)
                    ? null
                    : branchName);
            }
            return branchName!;
        }

        var updateBranch = updateRegistry.UpdateBranch;
        return string.IsNullOrEmpty(updateBranch) ? stableBranchName : updateBranch!;
    }

    public ProductBranch CreateBranch(string? branchName = null, string? manifestLocation = null)
    {
        var isDefault = true;
        
        if (string.IsNullOrEmpty(branchName))
            branchName = BranchManager.StableBranchName;
        else
            isDefault = ProductBranch.BranchNamEqualityComparer.Equals(branchName!, BranchManager.StableBranchName);

        if (string.IsNullOrEmpty(manifestLocation))
            return BranchManager.GetBranchFromName(branchName!);

        var manifestUri = new Uri(manifestLocation, UriKind.Absolute);

        return new ProductBranch(branchName!, [manifestUri], isDefault);
    }

    public abstract Task<UpdateCatalog> CheckForUpdateAsync(ProductBranch branch, CancellationToken token = default);

    public abstract Task UpdateAsync(UpdateCatalog updateCatalog, CancellationToken token = default);
}