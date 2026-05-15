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

    public string GetBranchNameFromRegistry(string? overrideBranchName, bool setRegistry)
    {
        var registry = ServiceProvider.GetRequiredService<IRegistry>();
        using var updateRegistry = new ApplicationUpdateRegistry(registry, Environment);

        var stableBranchName = BranchManager.StableBranchName;

        if (!string.IsNullOrEmpty(overrideBranchName))
        {
            if (setRegistry)
            {
                updateRegistry.SetBranch(ProductBranch.BranchNamEqualityComparer.Equals(overrideBranchName!, stableBranchName)
                    ? null
                    : overrideBranchName);
            }
            return overrideBranchName!;
        }

        var updateBranch = updateRegistry.UpdateBranch;
        Logger?.LogTrace("Branch name in registry: '{UpdateBranch}'", updateBranch);
        return string.IsNullOrEmpty(updateBranch) ? stableBranchName : updateBranch!;
    }

    public ProductBranch CreateBranch(string? branchName = null, string? manifestLocation = null)
    {
        var (resolvedBranchName, isDefault) = ResolveBranchName(branchName);

        if (string.IsNullOrEmpty(manifestLocation))
            return BranchManager.GetBranchFromName(resolvedBranchName);

        var manifestUri = new Uri(manifestLocation!, UriKind.Absolute);
        return new ProductBranch(resolvedBranchName, [manifestUri], isDefault);
    }

    public ProductBranch CreateBranchFromServerUrl(string serverBaseUrl, string? branchName = null)
    {
        if (string.IsNullOrEmpty(serverBaseUrl))
            throw new ArgumentException("Server base URL must not be empty.", nameof(serverBaseUrl));

        var (resolvedBranchName, isDefault) = ResolveBranchName(branchName);

        var serverUri = new Uri(serverBaseUrl, UriKind.Absolute);
        var manifestUri = ApplicationBranchUtilities.BuildManifestUri(serverUri, resolvedBranchName);
        return new ProductBranch(resolvedBranchName, [manifestUri], isDefault);
    }

    private (string Name, bool IsDefault) ResolveBranchName(string? branchName)
    {
        return string.IsNullOrEmpty(branchName) 
            ? (BranchManager.StableBranchName, true) 
            : (branchName, ProductBranch.BranchNamEqualityComparer.Equals(branchName!, BranchManager.StableBranchName));
    }

    public abstract Task<UpdateCatalog> CheckForUpdateAsync(ProductBranch branch, CancellationToken token = default);

    public abstract Task UpdateAsync(UpdateCatalog updateCatalog, CancellationToken token = default);
}