﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
internal class ImageDialogViewModel : IImageDialogViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public WindowState MinMaxState { get; set; }
    public bool RightToLeft { get; set; }
    public string? Title { get; set; }
    public bool IsFullScreen { get; set; }
    public bool IsResizable { get; set; }
    public bool HasMinimizeButton { get; set; }
    public bool HasMaximizeButton { get; set; }
    public bool IsGripVisible { get; set; }
    public bool ShowIcon { get; set; }
    public bool HasDialogFrame { get; set; }
    public bool IsCloseButtonEnabled { get; set; }
    public event EventHandler? CloseDialogRequest;
    public string? ResultButton { get; } = null;
    public IList<IButtonViewModel> Buttons { get; } = null!;
    public IDialogAdditionalInformationViewModel? AdditionalInformation { get; }

    public void CloseDialog()
    {
    }

    public void OnClosing(CancelEventArgs e)
    {
    }

    public ImageKey Image => ImageKeys.Github;
}