using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Updater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AnakinRaW.ApplicationBase.Update;

public abstract class ApplicationUpdater
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly IProductService ProductService;
    protected readonly IBranchManager BranchManager;
    protected readonly IUpdateService UpdateService;
    protected readonly ILogger? Logger;

    protected ApplicationUpdater(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        ProductService = ServiceProvider.GetRequiredService<IProductService>();
        BranchManager = ServiceProvider.GetRequiredService<IBranchManager>();
        UpdateService = ServiceProvider.GetRequiredService<IUpdateService>();
        Logger = ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
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

    public abstract Task<IUpdateCatalog> CheckForUpdateAsync(ProductBranch branch, CancellationToken token = default);

    public abstract Task UpdateAsync(IUpdateCatalog updateCatalog, CancellationToken token = default);
}