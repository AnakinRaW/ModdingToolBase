using System;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Updater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase;

internal class CommandLineToolSelfUpdater
{
    private readonly CommandLineUpdaterOptions _options;
    private readonly IApplicationEnvironment _appEnvironment;
    private readonly IProductService _productService;
    private readonly IResourceExtractor _resourceExtractor;
    private readonly IBranchManager _branchManager;
    private readonly IUpdateService _updateService;
    private readonly ILogger? _logger;

    public CommandLineToolSelfUpdater(CommandLineUpdaterOptions options, IServiceProvider serviceProvider)
    {
        _options = options;
        _appEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();
        _productService = serviceProvider.GetRequiredService<IProductService>();
        _resourceExtractor = serviceProvider.GetRequiredService<IResourceExtractor>();
        _branchManager = serviceProvider.GetRequiredService<IBranchManager>();
        _updateService = serviceProvider.GetRequiredService<IUpdateService>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public void UpdateIfNecessary()
    {
        Task.Run(UpdateIfNecessaryAsync).Wait();
    }

    public async Task UpdateIfNecessaryAsync()
    {
        if (_options.Skip)
            return;

        await _resourceExtractor.ExtractAsync(ExternalUpdater.ExternalUpdaterConstants.AppUpdaterModuleName,
            _appEnvironment.ApplicationLocalPath);

        var product = _productService.GetCurrentInstance();

        if (product.Branch is null)
            throw new InvalidOperationException("Current installation does not have a branch.");

        var branchToUse = _options.Branch ?? product.Branch;

        var branches = (await _branchManager.GetAvailableBranches()).ToList();
        var currentBranch = branches.FirstOrDefault(b => b.Equals(branchToUse));

        if (currentBranch is null)
            throw new InvalidOperationException($"Could not find branch '{branches}'");
        
        var updateRef = _productService.CreateProductReference(null, currentBranch);

        try
        {
            _logger?.LogInformation("Checking for updates...");
            await _updateService.CheckForUpdates(updateRef);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, $"Failed to check for updates: {ex.Message}");
        }

        //if (product.UpdateCatalog is null)
        //{
        //    _logger?.LogInformation("Nothing to update.");
        //    return;
        //}

        //await UpdateAsync(product.UpdateCatalog).ConfigureAwait(false);
    }

    private async Task UpdateAsync(IUpdateCatalog updateCatalog)
    {
    }
}