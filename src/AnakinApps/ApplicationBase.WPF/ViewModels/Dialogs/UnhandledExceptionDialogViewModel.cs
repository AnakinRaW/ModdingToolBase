using System;
using System.Windows.Input;
using System.Windows.Media;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.ViewModels.Dialogs;

public partial class UnhandledExceptionDialogViewModel : ModalWindowViewModel, IUnhandledExceptionDialogViewModel
{
    [ObservableProperty]
    private Exception _exception = null!;

    public string WindowCaption { get; }

    public string Header => "Oh no, something went wrong!";

    public string HandlerDescription => string.Empty;

    public ICommand? Handler => null;

    public ImageSource? HandlerIcon => null;

    public string HandlerName => string.Empty;

    public UnhandledExceptionDialogViewModel(Exception exception, IServiceProvider serviceProvider)
    {
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        HasMaximizeButton = false;
        HasMinimizeButton = false;
        IsResizable = false;
        IsCloseButtonEnabled = false;
        var env = serviceProvider.GetRequiredService<ApplicationEnvironment>();
        WindowCaption = env.ApplicationName;
    }
}
