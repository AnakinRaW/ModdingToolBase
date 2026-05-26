using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AnakinRaW.ApplicationBase.Commands.Handlers;
using AnakinRaW.ApplicationBase.Imaging;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnakinRaW.ApplicationBase;

/// <summary>
/// WPF-aware specialization of <see cref="SelfUpdateableAppLifecycle"/>: registers the WPF application
/// framework + update dialog wiring, treats a Shift held at startup as a reset request, surfaces reset
/// failures via a message box, and resolves a <see cref="WpfUnhandledExceptionHandler"/> at startup so
/// it can hook the AppDomain unhandled-exception event before any app code runs.
/// </summary>
public abstract class WpfSelfUpdateableAppLifecycle : SelfUpdateableAppLifecycle
{
    protected virtual ImageKey AppIcon => default;

    protected override void CreateAppServices(IServiceCollection services, IReadOnlyList<string> args)
    {
        base.CreateAppServices(services, args);

        services.AddApplicationFramework();
        services.AddWpfUpdateFramework(AppIcon);

        services.AddSingleton<IShowUpdateWindowCommandHandler>(sp => new ShowUpdateWindowCommandHandler(sp));
        services.AddSingleton<IUpdateDialogViewModelFactory>(sp => new ApplicationUpdateInteractionFactory(sp));
        services.AddSingleton(sp => new WpfUnhandledExceptionHandler(sp));

        services.Replace(ServiceDescriptor.Singleton<IModalWindowFactory>(sp => new ApplicationModalWindowFactory(sp)));
        services.Replace(ServiceDescriptor.Singleton<IDialogFactory>(sp => new ApplicationDialogFactory(sp)));

        ImageLibrary.Instance.LoadCatalog(ImageCatalog.Instance);
    }

    protected sealed override async Task<int> RunAppAsync(string[] args, IServiceProvider appServiceProvider)
    {
        // Resolve the unhandled-exception handler eagerly so its constructor hooks
        // AppDomain.UnhandledException before any user code in RunWpfAppAsync executes.
        using (appServiceProvider.GetRequiredService<WpfUnhandledExceptionHandler>())
        {
            return await RunWpfAppAsync(args, appServiceProvider).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// The WPF app entry point — called once services are built and the unhandled-exception handler is wired up.
    /// </summary>
    protected abstract Task<int> RunWpfAppAsync(string[] args, IServiceProvider appServiceProvider);

    protected override bool ShouldPreBootstrapResetApp(IReadOnlyList<string> args)
    {
        return base.ShouldPreBootstrapResetApp(args) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
    }

    protected override void ResetApp()
    {
        try
        {
            base.ResetApp();
        }
        catch (Exception ex)
        {
            // Reset failed — fall back to a plain MessageBox. The richer UnhandledExceptionDialog
            // requires a service provider that's probably already in a known-bad state at this point.
            MessageBox.Show(
                $"Resetting the application failed: {ex.Message}",
                "Reset failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
