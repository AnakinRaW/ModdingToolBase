﻿using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;

namespace AnakinRaW.ApplicationBase.ViewModels.Dialogs;

internal interface IKillProcessDialogViewModel : IImageDialogViewModel
{
    string Header { get; }

    IFileInfo LockedFile { get; }

    IEnumerable<ILockingProcess> LockingProcesses { get; }
}