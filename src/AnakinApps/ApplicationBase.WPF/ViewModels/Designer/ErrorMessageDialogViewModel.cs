﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using AnakinRaW.ApplicationBase.Imaging;
using AnakinRaW.ApplicationBase.ViewModels.Dialogs;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog.Buttons;
using AnakinRaW.CommonUtilities.Wpf.Imaging;

namespace AnakinRaW.ApplicationBase.ViewModels.Designer;

// ReSharper disable All 
#pragma warning disable CS0067
[EditorBrowsable(EditorBrowsableState.Never)]
internal class ErrorMessageDialogViewModel : IErrorMessageDialogViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? CloseDialogRequest;

    public WindowState MinMaxState { get; set; }
    public bool RightToLeft { get; set; }
    public string? Title { get; set; }
    public bool IsFullScreen { get; set; }
    public bool IsResizable { get; set; }
    public bool HasMinimizeButton { get; set; }
    public bool HasMaximizeButton { get; set; }
    public bool IsGripVisible { get; set; }
    public bool ShowIcon { get; set; }
    public string? ResultButton { get; }
    public IList<IButtonViewModel> Buttons { get; } = null!;
    public IDialogAdditionalInformationViewModel? AdditionalInformation { get; }
    public bool HasDialogFrame { get; set; }
    public bool IsCloseButtonEnabled { get; set; }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public void CloseDialog()
    {
    }

    public void OnClosing(CancelEventArgs e)
    {
    }

    public string Header => "This is a test message for the error window.";
    public string Message => "This is a very long message, which may represent an exception message containing a lot of details, end-users don't under stand anyway. However this is better than a hexed error code.";
    public ImageKey Image => ImageKeys.Github;
}