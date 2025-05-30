﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Text;
using Vanara.PInvoke;

namespace AnakinRaW.AppUpdaterFramework.FileLocking;

[SupportedOSPlatform("windows")]
internal class WindowsLockingProcessManager : IDisposable
{
    private readonly uint _sessionId;

    private bool _isDisposed;
    private bool _registered;

    private WindowsLockingProcessManager(uint sessionId)
    {
        _sessionId = sessionId;
    }

    ~WindowsLockingProcessManager()
    {
        DisposeCore();
    }

    [SupportedOSPlatform("windows")]
    public static WindowsLockingProcessManager Create()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Managing locked files is only supported on Windows");

        var strSessionKey = new StringBuilder(32);
        var result = RstrtMgr.RmStartSession(out var pSessionHandle, 0, strSessionKey);
        if (result != 0)
            throw new Win32Exception(result.ToHRESULT().Code);
        return new WindowsLockingProcessManager(pSessionHandle);
    }

    public void Dispose()
    {
        DisposeCore();
        GC.SuppressFinalize(this);
    }

    public void Register(IEnumerable<string>? files = null, IEnumerable<LockingProcessInfo>? processes = null)
    {
        var fileNames = files?.ToArray();
        var fileCount = (uint?) fileNames?.Length ?? 0;
        var processArray = processes?.ToArray();
        var processCount = (uint?) processArray?.Length ?? 0;
        if (fileCount == 0 && processCount == 0)
            return;
        _registered = true;
        var rgProcesses = processArray?.Select(Convert).ToArray();

        var result = RstrtMgr.RmRegisterResources(_sessionId, fileCount, fileNames, processCount, rgProcesses);
        if (result.Failed)
            throw new Win32Exception(result.ToHRESULT().Code);
    }

    public void TerminateRegisteredProcesses()
    {
        if (!_registered)
            return;
        var result = RstrtMgr.RmShutdown(_sessionId, RstrtMgr.RM_SHUTDOWN_TYPE.RmForceShutdown);
        if (result.Failed)
            throw new Win32Exception(result.ToHRESULT().Code);
    }

    public IEnumerable<LockingProcessInfo> GetProcesses()
    {
        if (!_registered) 
            return [];
        int result;
        var pnProcInfo = 0U;
        RstrtMgr.RM_PROCESS_INFO[]? rmProcessInfoArray = null;
        do
        {
#pragma warning disable CS8604 // Possible null reference argument.
            result = RstrtMgr.RmGetList(_sessionId, out var pnProcInfoNeeded, ref pnProcInfo, rmProcessInfoArray!, out _).ToHRESULT().Code;
#pragma warning restore CS8604 // Possible null reference argument.

            switch (result)
            {
                case 0:
                    break;
                case 234:
                    pnProcInfo = pnProcInfoNeeded;
                    rmProcessInfoArray = new RstrtMgr.RM_PROCESS_INFO[pnProcInfo];
                    break;
                default:
                    throw new Win32Exception(result);
            }
        } while (result == 234);

        if (rmProcessInfoArray != null && rmProcessInfoArray.Length != 0)
            return rmProcessInfoArray.Select(process => new LockingProcessInfo(process)).ToArray();
        return [];
    }

    private void DisposeCore()
    {
        if (_isDisposed)
            return;
        var result = RstrtMgr.RmEndSession(_sessionId);
        _isDisposed = true;
        if (result.Failed)
            throw new Win32Exception(result.ToHRESULT().Code);
    }

    private static RstrtMgr.RM_UNIQUE_PROCESS Convert(LockingProcessInfo process)
    {
        if (process == null) 
            throw new ArgumentNullException(nameof(process));
        var fileTimeUtc = process.StartTime.ToFileTimeUtc();
        var fileTime = new FILETIME
        {
            dwHighDateTime = (int)(fileTimeUtc >> 32),
            dwLowDateTime = (int)(fileTimeUtc & uint.MaxValue)
        };
        return new RstrtMgr.RM_UNIQUE_PROCESS
        {
            dwProcessId = process.Id,
            ProcessStartTime = fileTime
        };
    }
}