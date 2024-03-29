using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;

public abstract partial class LoadingViewModelBase(IServiceProvider serviceProvider)
    : ViewModelBase(serviceProvider), ILoadingViewModel
{
    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private string? _loadingText;
}