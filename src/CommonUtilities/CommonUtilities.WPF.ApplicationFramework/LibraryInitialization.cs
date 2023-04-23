using System.Windows.Threading;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.StatusBar;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;

public static class LibraryInitialization
{
    public static void AddApplicationFramework(this IServiceCollection serviceCollection)
    {
        // Must not be lazy!
        serviceCollection.AddSingleton<IAppDispatcher>(new AppDispatcher(Dispatcher.CurrentDispatcher));

        serviceCollection.AddSingleton<IWindowService>(_ => new WindowService());
        serviceCollection.AddSingleton<IApplicationShutdownService>(sp => new ApplicationShutdownService(sp));

        serviceCollection.AddSingleton<IQueuedDialogService>(sp => new QueuedDialogService(sp));
        serviceCollection.AddSingleton<IModalWindowService>(sp => new ModalWindowService(sp));

        serviceCollection.AddSingleton<IDialogButtonFactory>(_ => new DialogButtonFactory(true));
        serviceCollection.AddSingleton<IThemeManager>(sp => new ThemeManager(sp));
        serviceCollection.AddSingleton<IViewModelPresenter>(_ => new ViewModelPresenterService());

        var statusBarService = new StatusBarService();
        serviceCollection.AddSingleton<IStatusBarService>(statusBarService);
        serviceCollection.AddSingleton(statusBarService);
    }
}