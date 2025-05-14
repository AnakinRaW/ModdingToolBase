using System;
using System.Collections.Generic;
using System.Threading;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Detection;
using AnakinRaW.AppUpdaterFramework.Installer;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.AppUpdaterFramework.Updater.Progress;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.SimplePipeline;
using AnakinRaW.CommonUtilities.SimplePipeline.Progress;
using AnakinRaW.CommonUtilities.SimplePipeline.Steps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Updater.Tasks;

internal class InstallStep : PipelineStep, IComponentStep
{
    public event EventHandler<ProgressEventArgs<ComponentProgressInfo>>? Progress;

    private readonly UpdateConfiguration _updateConfiguration;
    private readonly IReadOnlyDictionary<string, string> _productVariables;
    private readonly UpdateAction _action;
    private readonly IInstallableComponent? _currentComponent;
    private readonly DownloadStep? _download;
    private readonly IInstallerFactory _installerFactory;
    private readonly IBackupManager _backupManager;

    private IInstallableComponent Component { get; }

    IProductComponent IComponentStep.Component => Component;

    internal InstallResult Result { get; private set; } = InstallResult.Success;

    public ProgressType Type => ProgressTypes.Install;
    
    public long Size => Component.InstallationSize.Total;

    public InstallStep(
        IInstallableComponent installable, 
        UpdateConfiguration updateConfiguration,
        IReadOnlyDictionary<string, string> productVariables,
        IServiceProvider serviceProvider) :
        this(installable, UpdateAction.Delete, updateConfiguration, productVariables, serviceProvider)
    {
    }

    public InstallStep(
        IInstallableComponent installable,
        IInstallableComponent? currentComponent, 
        DownloadStep download, 
        UpdateConfiguration updateConfiguration,
        IReadOnlyDictionary<string, string> productVariables,
        IServiceProvider serviceProvider) : 
        this(installable, UpdateAction.Update, updateConfiguration, productVariables, serviceProvider)
    {
        _currentComponent = currentComponent;
        _download = download ?? throw new ArgumentNullException(nameof(download));
    }

    private InstallStep(
        IInstallableComponent installable, 
        UpdateAction updateAction,
        UpdateConfiguration updateConfiguration,
        IReadOnlyDictionary<string, string> productVariables,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Component = installable ?? throw new ArgumentNullException(nameof(installable));

        _action = updateAction;
        _updateConfiguration = updateConfiguration ?? throw new ArgumentNullException(nameof(updateConfiguration));
        _productVariables = productVariables;
        _installerFactory = serviceProvider.GetRequiredService<IInstallerFactory>();
        _backupManager = serviceProvider.GetRequiredService<IBackupManager>();
    }

    public override string ToString()
    {
        return $"{_action}ing \"{Component.GetUniqueId()}\"";
    }

    protected override void RunCore(CancellationToken token)
    {
        _download?.Wait();
        if (_download?.Error != null)
        {
            Logger?.LogWarning($"Skipping {_action} of '{Component.GetUniqueId()}' since downloading it failed.");
            return;
        }

        var installer = _installerFactory.CreateInstaller(Component);
        installer.Progress += OnInstallerProgress;

        try
        {
            if (_action == UpdateAction.Update)
                ValidateEnoughDiskSpaceAvailable();

            if (_updateConfiguration.BackupPolicy != BackupPolicy.Disable)
                BackupComponent();

            switch (_action)
            {
                case UpdateAction.Update:
                {
                    if (_download is null)
                        throw new InvalidOperationException("download step must not be null");
                    var localPath = _download.DownloadPath;
                    Result = installer.Install(Component, localPath, _productVariables, token);
                    break;
                }
                case UpdateAction.Delete:
                    Result = installer.Remove(Component, _productVariables, token);
                    break;
            }

            if (Result == InstallResult.SuccessRestartRequired)
            {
                var restartManager = Services.GetRequiredService<IRestartManager>();
                restartManager.SetRestart(RestartType.ApplicationRestart);
                Logger?.LogWarning($"Component '{Component.GetDisplayName()}' get scheduled for installation after a restart.");
                
                var pendingComponentStore = Services.GetRequiredService<IWritablePendingComponentStore>();
                pendingComponentStore.AddComponent(new PendingComponent
                {
                    Component = Component, 
                    Action = _action
                });
            }

            if (Result == InstallResult.FailureElevationRequired)
            {
                Logger?.LogWarning($"Component '{Component.GetDisplayName()}' was not installed because required permissions are missing.");
                var restartManager = Services.GetRequiredService<IRestartManager>();
                restartManager.SetRestart(RestartType.ApplicationElevation);
            }

            if (Result.IsFailure())
                throw new StepFailureException([this]);
            if (Result == InstallResult.Cancel)
                throw new OperationCanceledException();

            if (_updateConfiguration.ValidateInstallation)
                Result = ValidateInstall();

        }
        catch (OutOfDiskSpaceException e)
        {
            Logger?.LogError(e, e.Message);
            Result = InstallResult.Failure;
            throw;
        }
        finally
        {
            installer.Progress -= OnInstallerProgress;
        }
    }

    private InstallResult ValidateInstall()
    {
        var detectorFactory = Services.GetRequiredService<IComponentInstallationDetector>();
        var isInstalled = detectorFactory.IsInstalled(Component, _productVariables);

        switch (_action)
        {
            case UpdateAction.Update when isInstalled:
            case UpdateAction.Delete when !isInstalled:
                return InstallResult.Success;
            default:
                Logger?.LogWarning($"Validation of installed component '{Component.GetDisplayName()}' failed.");
                return InstallResult.Failure;
        }
    }

    private void BackupComponent()
    {
        if (_action == UpdateAction.Keep)
            return;

        var componentToBackup = _action switch
        {
            UpdateAction.Update => _currentComponent ?? Component,
            UpdateAction.Delete => Component,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        try
        {
            _backupManager.BackupComponent(componentToBackup);
        }
        catch (Exception ex)
        {
            Logger?.LogWarning(ex, $"Creating backup of '{Component.Id}' failed.");
            if (_updateConfiguration.BackupPolicy == BackupPolicy.Required)
            {
                Logger?.LogError("Stopping install due to BackupPolicy");
                throw;
            }
        }
    }

    private void ValidateEnoughDiskSpaceAvailable()
    {
        if (_action == UpdateAction.Keep)
            return;
        var options = DiskSpaceCalculator.CalculationOptions.All;

        var installPath = Component is IPhysicalInstallable physicalInstallable ? physicalInstallable.InstallPath : null;
        if (!string.IsNullOrEmpty(installPath))
            installPath = StringTemplateEngine.ResolveVariables(installPath!, _productVariables);

        // We already downloaded it, no need to calculate again
        if (_download is not null) 
            options &= ~DiskSpaceCalculator.CalculationOptions.Download;

        if (_updateConfiguration.BackupPolicy == BackupPolicy.Disable)
            options &= ~DiskSpaceCalculator.CalculationOptions.Backup;

        var diskSpaceCalculator = Services.GetRequiredService<IDiskSpaceCalculator>();

        diskSpaceCalculator.ThrowIfNotEnoughDiskSpaceAvailable(Component, _currentComponent, installPath, options);
    }

    private void OnInstallerProgress(object sender, ComponentProgressEventArgs e)
    {
        Progress?.Invoke(this, e);
    }
}