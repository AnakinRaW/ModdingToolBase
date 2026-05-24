using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Handlers.Interaction;

public interface ILockedFileInteractionHandler
{
    LockedFileHandlerInteractionResult HandleLockedFile(string file, IEnumerable<ILockingProcess> lockingProcesses);

    void HandleError(string message);
}