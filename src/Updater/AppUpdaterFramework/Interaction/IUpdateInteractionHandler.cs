﻿using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.FileLocking.Interaction;

namespace AnakinRaW.AppUpdaterFramework.Interaction;

public interface IUpdateInteractionHandler
{
    LockedFileHandlerInteractionResult HandleLockedFile(IFileInfo file, IEnumerable<ILockingProcess> lockingProcesses);

    void HandleError(string message);
}