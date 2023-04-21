using AnakinRaW.AppUpdaterFramework.Commands.Factories;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Imaging;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.AppUpdaterFramework.Updater.Handlers;
using AnakinRaW.AppUpdaterFramework.ViewModels.Factories;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnakinRaW.AppUpdaterFramework;

public static class LibraryInitialization
{
    public static void AddUpdateGui(this IServiceCollection serviceCollection, ImageKey applicationIcon = default)
    {
        serviceCollection.AddUpdateFramework();

        serviceCollection.AddSingleton<IProductViewModelFactory>(sp => new ProductViewModelFactory(sp));
        serviceCollection.AddSingleton<IUpdateCommandsFactory>(sp => new UpdateCommandsFactory(sp));

        serviceCollection.Replace(ServiceDescriptor.Singleton<IUpdateHandler>(sp => new CommandUpdateHandler(sp)));
        serviceCollection.Replace(ServiceDescriptor.Singleton<IUpdateInteractionHandler>(sp => new DialogUpdateInteractionHandler(sp)));


        ImageLibrary.Instance.LoadCatalog(ImageCatalog.Instance);
        AppIconHolder.ApplicationIcon = applicationIcon;
    }
}