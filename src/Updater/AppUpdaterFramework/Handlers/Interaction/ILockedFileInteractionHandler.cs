using System.Collections.Generic;
using System.IO.Abstractions;

namespace AnakinRaW.AppUpdaterFramework.Handlers.Interaction;

public interface ILockedFileInteractionHandler
{
    LockedFileHandlerInteractionResult HandleLockedFile(IFileInfo file, IEnumerable<ILockingProcess> lockingProcesses);

    void HandleError(string message);
}