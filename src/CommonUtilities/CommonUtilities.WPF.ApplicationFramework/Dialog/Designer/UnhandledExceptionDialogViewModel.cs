﻿using System;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using AnakinRaW.CommonUtilities.Wpf.Controls;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog.Designer;

internal class UnhandledExceptionDialogViewModel : ModalWindowViewModel, IUnhandledExceptionDialogViewModel
{
    public Exception Exception => new TestException("Test Message", new StackTrace().ToString());

    public string WindowCaption => "Window Title";

    public string Header => "Oh no, something went wrong!";

    public string? HandlerDescription =>
        "This is a long text, telling the user what the custom button can do. This can be logging the error, etc.";

    public ICommand? Handler => new RoutedUICommand();
    public ImageSource? HandlerIcon => null;

    public string? HandlerName => "_Handle...";


    private class TestException(string message, string stackTrace) : Exception(message)
    {
        public override string StackTrace { get; } = stackTrace;
    }
}