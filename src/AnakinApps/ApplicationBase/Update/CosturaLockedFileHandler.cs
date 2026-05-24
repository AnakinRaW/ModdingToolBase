using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;

namespace AnakinRaW.ApplicationBase.Update;

public sealed class CosturaLockedFileHandler : ILockedFileInteractionHandler
{
    public LockedFileHandlerInteractionResult HandleLockedFile(string file, IEnumerable<ILockingProcess> lockingProcesses)
    {
        throw new InvalidOperationException("Costura Applications do not support locked files.");
    }

    public void HandleError(string message)
    {
    }
}