﻿namespace AnakinRaW.AppUpdaterFramework.FileLocking.Interaction;

public interface ILockingProcess
{
    string ProcessName { get; }

    uint ProcessId { get; }

    internal record struct LockingProcess(string ProcessName, uint ProcessId) : ILockingProcess;
}