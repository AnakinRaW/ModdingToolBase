using System;
using AnakinRaW.AppUpdaterFramework.Commands;
using AnakinRaW.AppUpdaterFramework.Commands.Factories;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Imaging;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;
using AnakinRaW.AppUpdaterFramework.ViewModels.Progress;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.Factories;

internal class ProductViewModelFactory(IServiceProvider serviceProvider) : IProductViewModelFactory
{
    private readonly IAppDispatcher _dispatcher = serviceProvider.GetRequiredService<IAppDispatcher>();
    private readonly IUpdateCommandsFactory _commandsFactory = serviceProvider.GetRequiredService<IUpdateCommandsFactory>();
    private readonly UpdateConfiguration _updateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();

    public IProductViewModel Create(IInstalledProduct product, IUpdateCatalog? updateCatalog)
    {
        IProductStateViewModel stateViewModel;
        ICommandDefinition? action = null;
        if (updateCatalog is null || updateCatalog.Action == UpdateCatalogAction.None)
        {
            if (product.State != ProductState.Installed && !_updateConfiguration.RestartConfiguration.SupportsRestart)
            {
                var message = product.State switch
                {
                    ProductState.RestartRequired => "The application needs to be restarted",
                    ProductState.ElevationRequired => "The application needs to run with admin rights",
                    _ => "The application needs to be restarted"
                };
                stateViewModel = new ErrorStateViewModel(product, message, serviceProvider);
            }
            else
            {
                action = product.State switch
                {
                    ProductState.RestartRequired => _commandsFactory.CreateRestart(),
                    ProductState.ElevationRequired => _commandsFactory.CreateElevate(),
                    _ => action
                };
                stateViewModel = new InstalledStateViewModel(product, serviceProvider);
            }
        }
        else if (updateCatalog.Action is UpdateCatalogAction.Install or UpdateCatalogAction.Uninstall)
        {
            stateViewModel = new ErrorStateViewModel(product, "Unable to get update information.", serviceProvider);
        }
        else
        {
            stateViewModel = new UpdateAvailableStateViewModel(product, updateCatalog, serviceProvider);
            var isRepair = ProductReferenceEqualityComparer.VersionAware.Equals(product, updateCatalog.UpdateReference);
            action = new UpdateCommand(updateCatalog, serviceProvider, isRepair);
        }

        return new ProductViewModel(product.Name, AppIconHolder.ApplicationIcon, stateViewModel, action, serviceProvider);
    }

    public IProductViewModel Create(IUpdateSession updateSession)
    {
        if (updateSession == null)
            throw new ArgumentNullException(nameof(updateSession));

        var cancelCommand = new CancelUpdateCommand(updateSession);
        var progressViewModel = CreateProgressViewModel(updateSession);
        var updatingViewModel = new UpdatingStateViewModel(progressViewModel, serviceProvider);

        return new ProductViewModel(updateSession.Product.Name, AppIconHolder.ApplicationIcon, updatingViewModel, cancelCommand, serviceProvider);
    }

    private IProgressViewModel CreateProgressViewModel(IUpdateSession updateSession)
    {
        return _dispatcher.Invoke(() =>
        {
            var downloadProgressBarViewModel = new DownloadingProgressBarViewModel(updateSession, serviceProvider);
            var installProgressBarViewModel = new InstallingProgressBarViewModel(updateSession, serviceProvider);
            return new ProgressViewModel(serviceProvider, downloadProgressBarViewModel, installProgressBarViewModel);
        });
    }
}