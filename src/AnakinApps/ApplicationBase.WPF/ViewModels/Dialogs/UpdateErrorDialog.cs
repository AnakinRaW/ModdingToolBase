using System;
using AnakinRaW.ApplicationBase.Imaging;
using AnakinRaW.CommonUtilities.Wpf.Imaging;

namespace AnakinRaW.ApplicationBase.ViewModels.Dialogs;

internal class UpdateErrorDialog(string message, IServiceProvider serviceProvider)
    : UpdateImageDialog(serviceProvider), IUpdateErrorDialog
{
    public override ImageKey Image => ImageKeys.Vader;

    public string Header => "Error while updating!";

    public string Message { get; } = message;
}