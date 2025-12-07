using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.InteropServices;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.FileLocking;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vanara.PInvoke;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

internal sealed class LockedFileHandler(IServiceProvider serviceProvider) : ILockedFileHandler
{
    private readonly UpdateConfiguration _updateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(LockedFileHandler));
    private readonly ILockedFileInteractionHandler _updateInteractionHandler = serviceProvider.GetRequiredService<ILockedFileInteractionHandler>();

    public ILockedFileHandler.Result Handle(IFileInfo file)
    {
        if (!file.Exists)
            throw new InvalidOperationException($"Expected '{file}' to exist.");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger?.LogWarning("Handling locked files is only supported for Windows applications!");
            return ILockedFileHandler.Result.Locked;
        }

        using var lockingProcessManager = WindowsLockingProcessManager.Create();
        lockingProcessManager.Register([file.FullName]);

        var lockingProcesses = lockingProcessManager.GetProcesses().ToList();

        if (!lockingProcesses.AnyRunning())
        {
            var e = new InvalidOperationException($"The file '{file}' is not locked by any process.");
            _logger?.LogTrace(e, e.Message);
            throw e;
        }

        var isLockedByApplication = lockingProcesses.ContainsCurrentProcess();

        //The file is locked by a system file
        if (lockingProcesses.Any(x => x.ApplicationType == RstrtMgr.RM_APP_TYPE.RmCritical && !x.IsCurrentProcess()))
        {
            HandleError("Files are locked by a system process that cannot be terminated. Please restart the system");
            return ILockedFileHandler.Result.Locked;
        }

        _logger?.LogTrace("Handling locked file '{FileInfo}'", file);

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
                _logger?.LogTrace("Interaction result: Locked file '{FileInfo}' shall not be unlocked.", file);
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

            _logger?.LogTrace("Source '{FileInfo}' is locked by current application. Restart is required.", file);
            return ILockedFileHandler.Result.RequiresRestart;
        }

        return ILockedFileHandler.Result.Unlocked;
    }

    private void HandleError(string message)
    {
        _updateInteractionHandler.HandleError(message);
    }

    private LockedFileHandlerInteractionResult PromptProcessKill(IFileInfo file, IEnumerable<LockingProcessInfo> lockingProcesses)
    {
        var processes = lockingProcesses.Select(x => (ILockingProcess) new ILockingProcess.LockingProcess(x.Description, x.Id));
        return _updateInteractionHandler.HandleLockedFile(file, processes);
    }
}