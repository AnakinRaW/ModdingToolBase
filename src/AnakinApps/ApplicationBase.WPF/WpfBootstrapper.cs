using AnakinRaW.ApplicationBase.Commands.Handlers;
using AnakinRaW.ApplicationBase.Imaging;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.AppUpdaterFramework.Updater.Handlers;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnakinRaW.ApplicationBase;

public abstract class WpfBootstrapper : BootstrapperBase
{
    protected virtual ImageKey AppIcon => default;

    protected override void CreateCoreServices(IServiceCollection serviceCollection)
    {
        base.CreateCoreServices(serviceCollection);
        serviceCollection.Replace(ServiceDescriptor.Singleton<IAppResetHandler>(sp => new WpfAppResetHandler(sp)));
        serviceCollection.Replace(ServiceDescriptor.Singleton<IUnhandledExceptionHandler>(sp => new WpfUnhandledExceptionHandler(sp)));
    }

    private protected override void CreateApplicationServices(IServiceCollection serviceCollection)
    {
        base.CreateApplicationServices(serviceCollection);

        serviceCollection.AddApplicationFramework();
        serviceCollection.AddUpdateGui(AppIcon);

        serviceCollection.AddSingleton<IShowUpdateWindowCommandHandler>(sp => new ShowUpdateWindowCommandHandler(sp));

        serviceCollection.AddSingleton(sp => new ApplicationUpdateInteractionFactory(sp));
        serviceCollection.AddSingleton<IUpdateDialogViewModelFactory>(sp => sp.GetRequiredService<ApplicationUpdateInteractionFactory>());

        serviceCollection.Replace(
            ServiceDescriptor.Singleton<IRestartHandler>(sp => new UpdateRestartCommandHandler(sp)));

        serviceCollection.TryAddSingleton<IModalWindowFactory>(sp => new ApplicationModalWindowFactory(sp));
        serviceCollection.TryAddSingleton<IDialogFactory>(sp => new ApplicationDialogFactory(sp));

        ImageLibrary.Instance.LoadCatalog(ImageCatalog.Instance);
    }
}