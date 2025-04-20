using AnakinRaW.AppUpdaterFramework.Commands.Factories;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using AnakinRaW.AppUpdaterFramework.Imaging;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.AppUpdaterFramework.ViewModels.Factories;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnakinRaW.AppUpdaterFramework;

public static class LibraryInitialization
{
    public static void AddWpfUpdateFramework(this IServiceCollection serviceCollection, ImageKey applicationIcon = default)
    {
        ImageLibrary.Instance.LoadCatalog(ImageCatalog.Instance);
        AppIconHolder.ApplicationIcon = applicationIcon;

        // All internal
        serviceCollection.AddSingleton<IProductViewModelFactory>(sp => new ProductViewModelFactory(sp));
        serviceCollection.AddSingleton<IUpdateCommandsFactory>(sp => new UpdateCommandsFactory(sp));

        // Internal implementation
        serviceCollection.AddSingleton<IUpdateResultInteractionHandler>(sp => new DialogResultInteractionHandler(sp));

        serviceCollection.Replace(ServiceDescriptor.Singleton<IUpdateHandler>(sp => new CommandUpdateHandler(sp)));
        serviceCollection.Replace(ServiceDescriptor.Singleton<IUpdateInteractionHandler>(sp => new DialogUpdateInteractionHandler(sp)));

        // Overrides
        serviceCollection.Replace(ServiceDescriptor.Singleton<IRestartHandler>(sp => new UpdateRestartCommandHandler(sp)));
    }
}