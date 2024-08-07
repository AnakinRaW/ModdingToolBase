﻿using System;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Controls;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;

public abstract class ApplicationBase : Application
{
    protected IServiceProvider ServiceProvider { get; }
    protected ILogger? Logger { get; }

    protected ApplicationBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    protected sealed override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        PreWindowInitialize();
        var applicationViewModel = CreateApplicationViewModel();
        var windowService = InitializeWindow(applicationViewModel);
        windowService.ShowWindow();
        PostWindowInitialize(applicationViewModel);
        OnApplicationStarted();
    }

    protected abstract IApplicationViewModel CreateApplicationViewModel();

    protected abstract ApplicationMainWindow CreateMainWindow(IMainWindowViewModel viewModel);

    protected virtual void OnApplicationStarted()
    {
    }

    protected virtual void OnShutdownPrevented(object? sender, ShutdownPrevention e)
    {
    }

    protected virtual void InitializeServicesPostWindow(IApplicationViewModel viewModel)
    {
    }

    protected virtual void InitializeResources()
    {
    }

    protected virtual void InitializeServices()
    {
    }

    protected static T LoadResourceValue<T>(string xamlName)
    {
        return (T)LoadComponent(new Uri(Assembly.GetCallingAssembly().GetName().Name + ";component/" + xamlName,
            UriKind.Relative));
    }

    private IWindowService InitializeWindow(IMainWindowViewModel viewModel)
    {
        var window = CreateMainWindow(viewModel);
        MainWindow = window;
        var service = ServiceProvider.GetRequiredService<IWindowService>();
        service.SetMainWindow(window);
        return service;
    }

    private void PreWindowInitialize()
    {
        InitializeResources();
        InitializeServices();
    }

    private void PostWindowInitialize(IApplicationViewModel viewModel)
    {
        var shutdownService = ServiceProvider.GetRequiredService<IApplicationShutdownService>();
        shutdownService.ShutdownRequested += (_, exitCode) => Dispatcher.BeginInvoke(DispatcherPriority.Normal, () =>
        {
            viewModel.Dispose();
            Shutdown(exitCode);
        });
        shutdownService.ShutdownPrevented += OnShutdownPrevented;
        InitializeServicesPostWindow(viewModel);
        viewModel.InitializeAsync().Wait();
    }
}