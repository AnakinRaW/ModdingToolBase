using System;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.StatusBar;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;

public abstract partial class ApplicationViewModelBase : MainWindowViewModel, IApplicationViewModel
{
    protected IServiceProvider ServiceProvider;
    protected ILogger? Logger;

    private bool _isDisposed;

    [ObservableProperty] private IViewModel? _currentViewModel;

    protected ApplicationViewModelBase(IStatusBarViewModel statusBarViewModel, IServiceProvider serviceProvider) : base(statusBarViewModel)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public abstract Task InitializeAsync();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void OnDispose()
    {
    }

    private void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;
        if (disposing)
            OnDispose();
        _isDisposed = true;
    }
}