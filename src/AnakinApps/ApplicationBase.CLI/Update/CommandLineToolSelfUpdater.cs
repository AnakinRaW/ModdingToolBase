using System;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Options;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Updater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Update;

internal class CommandLineToolSelfUpdater
{
    private readonly IApplicationEnvironment _appEnvironment;
    private readonly IProductService _productService;
    private readonly IResourceExtractor _resourceExtractor;
    private readonly IBranchManager _branchManager;
    private readonly IUpdateService _updateService;
    private readonly IUpdateHandler _updateHandler;
    private readonly ILogger? _logger;

    public CommandLineToolSelfUpdater(IServiceProvider serviceProvider)
    {
        _appEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();
        _productService = serviceProvider.GetRequiredService<IProductService>();
        _resourceExtractor = serviceProvider.GetRequiredService<IResourceExtractor>();
        _branchManager = serviceProvider.GetRequiredService<IBranchManager>();
        _updateService = serviceProvider.GetRequiredService<IUpdateService>();
        _updateHandler = serviceProvider.GetRequiredService<IUpdateHandler>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public int UpdateIfNecessary(IUpdaterCommandLineOptions options)
    {
        return Task.Run(async () => await UpdateIfNecessaryAsync(options)).GetAwaiter().GetResult();
    }

    public async Task<int> UpdateIfNecessaryAsync(IUpdaterCommandLineOptions options)
    {
        if (options.SkipUpdate)
        {
            _logger?.LogDebug("Update skipped.");
            return 0;
        }

        await _resourceExtractor.ExtractAsync(ExternalUpdater.ExternalUpdaterConstants.AppUpdaterModuleName,
            _appEnvironment.ApplicationLocalPath);

        var product = _productService.GetCurrentInstance();

        if (product.Branch is null)
            throw new InvalidOperationException("Current installation does not have a branch.");

        var branchToUse = options.UpdateBranch ?? product.Branch;

        var branches = (await _branchManager.GetAvailableBranches()).ToList();
        var currentBranch = branches.FirstOrDefault(b => b.Equals(branchToUse));

        if (currentBranch is null)
            throw new InvalidOperationException($"Could not find branch '{branchToUse}' in branch list '{string.Join(",", branches)}'");

        var updateRef = _productService.CreateProductReference(null, currentBranch);

        IUpdateCatalog? updateCatalog = null;
        try
        {
            _logger?.LogDebug("Checking for updates...");
            Console.WriteLine("Checking for updates...");
            updateCatalog = await _updateService.CheckForUpdatesAsync(updateRef);
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, $"Failed to check for updates: {ex.Message}");
        }

        if (updateCatalog?.Action != UpdateCatalogAction.Update)
        {
            Console.WriteLine("Nothing to update.");
            _logger?.LogDebug("Nothing to update.");
            return 0;
        }

        Console.WriteLine("Updating application...");
        try
        {
            await _updateHandler.UpdateAsync(updateCatalog).ConfigureAwait(false);
        }
        finally
        {
            Console.WriteLine("Update completed.");
        }

        var productState = _productService.GetCurrentInstance().State;

        if (productState is ProductState.RestartRequired)
            return RestartConstants.RestartRequiredCode;

        if (productState is ProductState.ElevationRequired)
            return -123;

        return 0;
    }
}