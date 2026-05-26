using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;

namespace AnakinRaW.ApplicationBase.ViewModels.Dialogs;

internal interface IKillProcessDialogViewModel : IImageDialogViewModel
{
    string Header { get; }

    string LockedFile { get; }

    IEnumerable<ILockingProcess> LockingProcesses { get; }
}