using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using AnakinRaW.ApplicationBase.Imaging;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog.Buttons;
using AnakinRaW.CommonUtilities.Wpf.Imaging;

namespace AnakinRaW.ApplicationBase.ViewModels.Dialogs;

internal class KillProcessDialogViewModel(
    IFileInfo lockedFile,
    IEnumerable<ILockingProcess> lockingProcesses,
    IServiceProvider serviceProvider)
    : UpdateImageDialog(serviceProvider), IKillProcessDialogViewModel
{
    internal const string KillButtonIdentifier = "kill";

    public string Header => $"Source '{LockedFile.Name}' is locked.";

    public IFileInfo LockedFile { get; } = lockedFile;

    public IEnumerable<ILockingProcess> LockingProcesses { get; } = lockingProcesses;

    public override ImageKey Image => ImageKeys.SwPulp;

    protected override IList<IButtonViewModel> CreateButtons(IDialogButtonFactory buttonFactory)
    {
        var killText = LockingProcesses.Count() == 1 ? "Kill Process" : "Kill Processes";
        var buttons = new List<IButtonViewModel>
        {
            buttonFactory.CreateCustom(KillButtonIdentifier, DialogButtonCommandsDefinitions.Create(killText)),
            buttonFactory.CreateRetry(false),
            buttonFactory.CreateCancel(true)
        };
        return buttons;
    }
}