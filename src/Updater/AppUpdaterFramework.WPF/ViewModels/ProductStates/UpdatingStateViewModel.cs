using System;
using AnakinRaW.AppUpdaterFramework.ViewModels.Progress;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.ViewModels;

namespace AnakinRaW.AppUpdaterFramework.ViewModels.ProductStates;

internal class UpdatingStateViewModel(IProgressViewModel progressViewModel, IServiceProvider serviceProvider)
    : ViewModelBase(serviceProvider), IUpdatingStateViewModel
{
    public IProgressViewModel ProgressViewModel { get; } = progressViewModel ?? throw new ArgumentNullException(nameof(progressViewModel));
}