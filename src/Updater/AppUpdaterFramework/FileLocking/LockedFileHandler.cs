using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace AnakinRaW.AppUpdaterFramework.FileLocking;

internal class LockedFileHandler(IServiceProvider serviceProvider) : InteractiveHandlerBase(serviceProvider), ILockedFileHandler
{
    private readonly UpdateConfiguration _updateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();

    public ILockedFileHandler.Result Handle(IFileInfo file)
    {
        if (!file.Exists)
            throw new InvalidOperationException($"Expected '{file}' to exist.");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Logger?.LogWarning("Handling locked files is only supported for Windows applications!");
            return ILockedFileHandler.Result.Locked;
        }

        using var lockingProcessManager = WindowsLockingProcessManager.Create();
        lockingProcessManager.Register([file.FullName]);

        var lockingProcesses = lockingProcessManager.GetProcesses().ToList();

        if (!lockingProcesses.AnyRunning())
        {
            var e = new InvalidOperationException($"The file '{file}' is not locked by any process.");
            Logger?.LogTrace(e, e.Message);
            throw e;
        }

        var isLockedByApplication = lockingProcesses.ContainsCurrentProcess();

        //The file is locked by a system file
        if (lockingProcesses.Any(x => x.ApplicationType == RstrtMgr.RM_APP_TYPE.RmCritical && !x.IsCurrentProcess()))
        {
            HandleError("Files are locked by a system process that cannot be terminated. Please restart the system");
            return ILockedFileHandler.Result.Locked;
        }

        Logger?.LogTrace($"Handling locked file '{file}'");

        var processesWithoutSelf = lockingProcesses.WithoutCurrentProcess().WithoutDebugger().WithoutStopped().ToList();

        if (processesWithoutSelf.Count > 0)
        {
            var interactionResult = LockedFileHandlerInteractionResult.Retry;
            do
            {
                processesWithoutSelf = lockingProcessManager.GetProcesses().WithoutCurrentProcess().WithoutDebugger().WithoutStopped().ToList();
                if (!processesWithoutSelf.Any())
                    break;
                interactionResult = PromptProcessKill(file, processesWithoutSelf);
            } while (interactionResult == LockedFileHandlerInteractionResult.Retry);

            // Interaction indicated to abort handling
            if (interactionResult == LockedFileHandlerInteractionResult.Cancel)
            {
                Logger?.LogTrace($"Interaction result: Locked file '{file}' shall not be unlocked.");
                return ILockedFileHandler.Result.Locked;
            }

            // Interaction indicated to kill the processes
            if (interactionResult == LockedFileHandlerInteractionResult.Kill)
            {
                using var managerWithoutSelf = WindowsLockingProcessManager.Create();
                managerWithoutSelf.Register(null, processesWithoutSelf);
                managerWithoutSelf.TerminateRegisteredProcesses();
            }

            // Source is still locked
            if (!lockingProcessManager.GetProcesses().WithoutCurrentProcess().WithoutDebugger().AllStopped())
                return ILockedFileHandler.Result.Locked;
        }

        if (isLockedByApplication)
        {
            if (!_updateConfiguration.RestartConfiguration.SupportsRestart)
                return ILockedFileHandler.Result.Locked;

            Logger?.LogTrace($"Source '{file}' is locked by current application. Restart is required.");
            return ILockedFileHandler.Result.RequiresRestart;
        }

        return ILockedFileHandler.Result.Unlocked;
    }

    private LockedFileHandlerInteractionResult PromptProcessKill(IFileInfo file, IEnumerable<ILockingProcessInfo> lockingProcesses)
    {
        var processes = lockingProcesses.Select(x => (ILockingProcess) new ILockingProcess.LockingProcess(x.Description, x.Id));
        return UpdateInteractionHandler.HandleLockedFile(file, processes);
    }
}