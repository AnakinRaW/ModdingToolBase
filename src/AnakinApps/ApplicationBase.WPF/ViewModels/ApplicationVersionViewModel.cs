using System;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.ViewModels;

internal class ApplicationVersionViewModel : ObservableObject, IDialogAdditionalInformationViewModel
{
    public string Version { get; }

    public ApplicationVersionViewModel(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        Version = serviceProvider.GetRequiredService<IApplicationEnvironment>().AssemblyInfo.InformationalVersion ?? "NO VERSION";
    }
}