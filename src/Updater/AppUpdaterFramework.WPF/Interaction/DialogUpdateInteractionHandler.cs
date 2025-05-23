﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Interaction;

internal class DialogUpdateInteractionHandler : ILockedFileInteractionHandler
{
    private readonly IQueuedDialogService _dialogService;
    private readonly IUpdateDialogViewModelFactory _viewModelFactory;

    public DialogUpdateInteractionHandler(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _dialogService = serviceProvider.GetRequiredService<IQueuedDialogService>();
        _viewModelFactory = serviceProvider.GetRequiredService<IUpdateDialogViewModelFactory>();
    }

    public LockedFileHandlerInteractionResult HandleLockedFile(IFileInfo file, IEnumerable<ILockingProcess> lockingProcesses)
    {
        var model = _viewModelFactory.CreateKillProcessesViewModel(file, lockingProcesses);
        var result = ShowDialog(model);
        return result switch
        {
            DefaultDialogButtonIdentifiers.Retry => LockedFileHandlerInteractionResult.Retry,
            UpdateDialogButtonIdentifiers.KillButtonIdentifier => LockedFileHandlerInteractionResult.Kill,
            _ => LockedFileHandlerInteractionResult.Cancel
        };
    }

    public void HandleError(string message)
    {
        var model = _viewModelFactory.CreateErrorViewModel(message);
        ShowDialog(model);
    }

    private string? ShowDialog(IDialogViewModel viewModel)
    {
        var task = Task.Run(async () => await _dialogService.ShowDialog(viewModel));
        task.Wait();
        return task.Result;
    }
}