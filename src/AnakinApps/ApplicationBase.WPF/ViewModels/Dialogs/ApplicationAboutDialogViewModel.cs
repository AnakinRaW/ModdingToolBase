using System;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.ViewModels.Dialogs;

public class ApplicationAboutDialogViewModel : DialogViewModel
{
    public override IDialogAdditionalInformationViewModel? AdditionalInformation { get; }

    public ApplicationAboutDialogViewModel(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        IsResizable = false;
        HasMaximizeButton = false;
        HasMinimizeButton = false;
        HasDialogFrame = true;
        AdditionalInformation = new ApplicationVersionViewModel(serviceProvider);

        var appName = serviceProvider.GetRequiredService<IApplicationEnvironment>().ApplicationName;
        Title = $"About {appName}";
    }
}