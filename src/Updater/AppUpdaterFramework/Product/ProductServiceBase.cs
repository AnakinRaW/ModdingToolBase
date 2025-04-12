using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Updater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Semver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;

namespace AnakinRaW.AppUpdaterFramework.Product;

public abstract class ProductServiceBase : IProductService
{ 
    private bool _isInitialized;
    private InstalledProduct? _installedProduct;

    private readonly object _syncLock = new();
    private readonly IRestartManager _restartManager;

    public abstract IDirectoryInfo InstallLocation { get; }

    protected ILogger? Logger { get; }

    protected IServiceProvider ServiceProvider { get; }

    protected ProductServiceBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _restartManager = serviceProvider.GetRequiredService<IRestartManager>();
        _restartManager.RestartRequired += OnRestartRequired!;
        serviceProvider.GetRequiredService<IUpdateService>().UpdateCompleted += OnUpdateCompleted!;
    }

    public IInstalledProduct GetCurrentInstance()
    {
        Initialize();
        lock (_syncLock)
        {
            return _installedProduct!;
        }
    }

    public IInstalledComponentsCatalog GetInstalledComponents()
    {
        Initialize();
        var currentInstance = GetCurrentInstance();
        var detectionService = ServiceProvider.GetRequiredService<IManifestInstallationDetector>();
        var installedComponents = detectionService.DetectInstalledComponents(currentInstance.Manifest, currentInstance.Variables);
        return new InstalledComponentsCatalog(currentInstance, installedComponents);
    }

    public virtual IProductReference CreateProductReference(SemVersion? newVersion, ProductBranch? newBranch)
    {
        var current = GetCurrentInstance();
        var branch = current.Branch;
        if (newBranch is not null)
            branch = newBranch;
        var version = current.Version;
        if (newVersion is not null)
            version = newVersion;
        return new ProductReference(current.Name, version, branch);
    }

    protected abstract IProductReference CreateCurrentProductReference();

    protected abstract IProductManifest GetManifestForInstalledProduct(
        IProductReference installedProduct, 
        IReadOnlyDictionary<string, string> productVariables);

    protected virtual void AddAdditionalProductVariables(IDictionary<string, string> variables, IProductReference product)
    {
    }

    private ProductState FetchInstallState()
    {
        var state = ProductState.Installed;

        if (_restartManager.RequiredRestartType == RestartType.ApplicationRestart)
            state = ProductState.RestartRequired;
        if (_restartManager.RequiredRestartType == RestartType.ApplicationElevation)
            state = ProductState.ElevationRequired;
        return state;
    }

    private void Initialize()
    {
        if (_isInitialized)
            return;
        Reset();
        _isInitialized = true;
    }

    private void Reset()
    {
        lock (_syncLock)
        {
            var installedProduct = BuildProduct();
            if (installedProduct is null)
            {
                var ex = new InvalidOperationException("Created Product must not be null!");
                Logger?.LogError(ex, ex.Message);
                throw ex;
            }
            _installedProduct = installedProduct;
        }
    }

    private InstalledProduct BuildProduct()
    {
        var productReference = CreateCurrentProductReference();
        var variables = AddProductVariables(productReference);
        var manifest = GetManifestForInstalledProduct(productReference, variables);
        var state = FetchInstallState();
        return new InstalledProduct(productReference, InstallLocation.FullName, manifest, variables, state);
    }

    private IReadOnlyDictionary<string, string> AddProductVariables(IProductReference product)
    {
        var variables = new Dictionary<string, string>();
        var installLocation = InstallLocation;
        variables.Add(KnownProductVariablesKeys.InstallDir, installLocation.FullName);
        variables.Add(KnownProductVariablesKeys.InstallDrive, installLocation.Root.FullName);

        // TODO: To ProductVariables, as this is not part of a template engine
        // We do not include Windows folder as it is not a special folder,
        // because this library shall not by default support operating system related folders. 
        // Custom properties can be used to support that individually.
        //private static readonly IDictionary<string, string> SpecialFolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        //{
        //    [ProgramFiles] = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        //    [ProgramFilesX64] = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        //    [CommonProgramFiles] = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86),
        //    [CommonProgramFilesX64] = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
        //    [ProgramData] = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        //};

        AddAdditionalProductVariables(variables, product);
        return new ReadOnlyDictionary<string, string>(variables);
    }

    private void OnRestartRequired(object sender, EventArgs e)
    {
        if (_installedProduct is null)
            return;
        if (_restartManager.RequiredRestartType == RestartType.ApplicationRestart)
        {
            _installedProduct!.State = ProductState.RestartRequired;
            Logger?.LogTrace("Restart required for current instance.");
        }
        else if (_restartManager.RequiredRestartType == RestartType.ApplicationElevation)
        {
            _installedProduct!.State = ProductState.ElevationRequired;
            Logger?.LogTrace("Elevation required for current instance.");
        }
    }

    private void OnUpdateCompleted(object sender, EventArgs e)
    {
        Reset();
    }
}