﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Windows;
using AnakinRaW.ApplicationBase.ViewModels.Dialogs;
using AnakinRaW.AppUpdaterFramework.FileLocking.Interaction;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog.Buttons;
using AnakinRaW.CommonUtilities.Wpf.Imaging;
using Testably.Abstractions;

namespace AnakinRaW.ApplicationBase.ViewModels.Designer;

// ReSharper disable All 
#pragma warning disable CS0067
[EditorBrowsable(EditorBrowsableState.Never)]
internal class DesignerKillProcessDialogViewModel : IKillProcessDialogViewModel
{
    private readonly IFileSystem _fileSystem = new RealFileSystem();
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
    public void CloseDialog()
    {
        throw new NotImplementedException();
    }

    public void OnClosing(CancelEventArgs e)
    {
        throw new NotImplementedException();
    }

    public bool HasDialogFrame { get; set; }
    public bool IsCloseButtonEnabled { get; set; }
    public string? ResultButton { get; } = null;
    public IList<IButtonViewModel> Buttons { get; } = null!;
    public IDialogAdditionalInformationViewModel? AdditionalInformation { get; } = null;
    public ImageKey Image { get; } = default;
    public string Header => "Source is locked";
    public IFileInfo LockedFile => _fileSystem.FileInfo.New("C:\\test.txt");
    public IEnumerable<ILockingProcess> LockingProcesses => new List<ILockingProcess> { new ProcessInfo() };

    private struct ProcessInfo : ILockingProcess
    {
        public string ProcessName => "Name";
        public uint ProcessId => 123;
    }
}