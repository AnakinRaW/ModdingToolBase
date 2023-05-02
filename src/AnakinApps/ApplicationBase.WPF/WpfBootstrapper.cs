using AnakinRaW.ApplicationBase.Commands.Handlers;
using AnakinRaW.ApplicationBase.Imaging;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnakinRaW.ApplicationBase;

public abstract class WpfBootstrapper : BootstrapperBase
{
    protected virtual ImageKey AppIcon => default;

    private protected override void CreateCoreServices(IServiceCollection serviceCollection)
    {
        base.CreateCoreServices(serviceCollection);
        serviceCollection.Replace(ServiceDescriptor.Singleton<IAppResetHandler>(sp => new WpfAppResetHandler(sp)));
        serviceCollection.Replace(ServiceDescriptor.Singleton<IUnhandledExceptionHandler>(sp => new WpfUnhandledExceptionHandler(sp)));
    }

    private protected override void CreateApplicationServices(IServiceCollection serviceCollection)
    {
        base.CreateApplicationServices(serviceCollection);

        serviceCollection.AddApplicationFramework();
        serviceCollection.AddWpfUpdateFramework(AppIcon);

        serviceCollection.AddSingleton<IShowUpdateWindowCommandHandler>(sp => new ShowUpdateWindowCommandHandler(sp));
        serviceCollection.AddSingleton<IUpdateDialogViewModelFactory>(sp => new ApplicationUpdateInteractionFactory(sp));

        serviceCollection.AddSingleton<IUpdateHandler>(sp => new CommandUpdateHandler(sp));

        serviceCollection.Replace(ServiceDescriptor.Singleton<IModalWindowFactory>(sp => new ApplicationModalWindowFactory(sp)));
        serviceCollection.Replace(ServiceDescriptor.Singleton<IDialogFactory>(sp => new ApplicationDialogFactory(sp)));
        
        ImageLibrary.Instance.LoadCatalog(ImageCatalog.Instance);
    }
}