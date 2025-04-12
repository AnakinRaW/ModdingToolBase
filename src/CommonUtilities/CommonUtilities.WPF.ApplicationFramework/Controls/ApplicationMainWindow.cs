using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Converters;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.StatusBar;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;
using AnakinRaW.CommonUtilities.Wpf.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Controls;

public class ApplicationMainWindow : ThemedWindow
{ 
    private ContentControl? _statusBarHost;

    protected readonly ILogger? Logger;

    public IServiceProvider ServiceProvider { get; }

    public new IMainWindowViewModel ViewModel { get; }


    static ApplicationMainWindow()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ApplicationMainWindow), new FrameworkPropertyMetadata(typeof(ApplicationMainWindow)));
        Keyboard.DefaultRestoreFocusMode = RestoreFocusMode.None;
    }

    public ApplicationMainWindow(IMainWindowViewModel viewModel, IServiceProvider serviceProvider) : base(viewModel)
    {
        ViewModel = viewModel;
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        serviceProvider.GetRequiredService<StatusBarService>().StatusBarModel = viewModel.StatusBar;
        SetBindings();
        DataContextChanged += OnDataContextChanged;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (User32.IsWindow(handle))
            User32.ShowOwnedPopups(handle, IsVisible);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _statusBarHost = GetTemplateChild<ContentControl>("PART_StatusBarHost")!;
        if (ViewModel.StatusBar.IsVisible)
        {
            _statusBarHost.Content = CreateStatusBarView();
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not IMainWindowViewModel mainWindowViewModel)
            return;
        _statusBarHost!.Content = !mainWindowViewModel.StatusBar.IsVisible ? null : CreateStatusBarView();
    }

    private FrameworkElement? CreateStatusBarView()
    {
        var factory = ServiceProvider.GetService<IStatusBarFactory>();
        if (factory == null)
        {
            Logger?.LogTrace("No IStatusBarFactory registered.");
            return null;
        }
        return factory.CreateStatusBar(ViewModel.StatusBar);
    }

    private void SetBindings()
    {
        TaskbarItemInfo = new TaskbarItemInfo();
        BindingOperations.SetBinding(TaskbarItemInfo, TaskbarItemInfo.ProgressStateProperty, new Binding
        {
            Path = new PropertyPath("ProgressState"),
            Converter = new BiDirectionalEnumConverter()
        });
    }
}