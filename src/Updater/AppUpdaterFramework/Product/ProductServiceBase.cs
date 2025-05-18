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
    private readonly object _syncLock = new();
    private readonly IRestartManager _restartManager;
    protected readonly ILogger? Logger;
    protected readonly IServiceProvider ServiceProvider;

    private bool _isInitialized;
    private InstalledProduct? _installedProduct;

    public abstract IDirectoryInfo InstallLocation { get; }

    protected ProductServiceBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _restartManager = serviceProvider.GetRequiredService<IRestartManager>();
        _restartManager.RestartRequired += OnRestartRequired;
        serviceProvider.GetRequiredService<IUpdateService>().UpdateCompleted += OnUpdateCompleted;
    }

    public IInstalledProduct GetCurrentInstance()
    {
        Initialize();
        lock (_syncLock)
        {
            return _installedProduct!;
        }
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

    public void UpdateComponentDetectionState()
    {
        Initialize();
        var currentInstance = GetCurrentInstance();
        DetectManifest(currentInstance.Manifest, currentInstance.Variables);
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
        DetectManifest(manifest, variables);
        var state = FetchInstallState();
        return new InstalledProduct(productReference, InstallLocation.FullName, manifest, variables, state);
    }

    private void DetectManifest(IProductManifest manifest, IReadOnlyDictionary<string, string> variables)
    {
        var detectionService = ServiceProvider.GetRequiredService<IManifestInstallationDetector>();
        detectionService.DetectInstalledComponents(manifest, variables);
    }

    private IReadOnlyDictionary<string, string> AddProductVariables(IProductReference product)
    {
        var variables = new Dictionary<string, string>();
        var installLocation = InstallLocation;
        variables.Add(KnownProductVariablesKeys.InstallDir, installLocation.FullName);
        variables.Add(KnownProductVariablesKeys.InstallDrive, installLocation.Root.FullName);

        // We do not include Windows folder as a special folder,
        // because this library shall not by default support operating system related folders. 
        // Application classes can use AddAdditionalProductVariables() to support that if required.
        variables.Add(KnownProductVariablesKeys.ProgramFiles, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        variables.Add(KnownProductVariablesKeys.CommonProgramFiles, Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86));
        variables.Add(KnownProductVariablesKeys.ProgramData, Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));

        if (Environment.Is64BitOperatingSystem)
        {
            variables.Add(KnownProductVariablesKeys.ProgramFilesX64, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            variables.Add(KnownProductVariablesKeys.CommonProgramFilesX64, Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles));
        }
        
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

    private void OnUpdateCompleted(object sender, UpdateResult? e)
    {
        Reset();
    }
}