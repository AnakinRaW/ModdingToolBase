﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using Vanara.PInvoke;

namespace AnakinRaW.AppUpdaterFramework.Elevation;

public sealed class ProcessElevation : IProcessElevation
{
    private readonly Lazy<bool> _isElevated;

    public bool IsElevated => _isElevated.Value;

    public ProcessElevation()
    {
        _isElevated = new Lazy<bool>(IsProcessElevated);
    }

    private static bool IsProcessElevated()
    {
        using var currentProcess = Process.GetCurrentProcess();
        using var processToken = OpenProcessToken(currentProcess.Handle);
        try
        {
            var elevation = AdvApi32.GetTokenInformation<AdvApi32.TOKEN_ELEVATION>(processToken, AdvApi32.TOKEN_INFORMATION_CLASS.TokenElevation);
            return elevation.TokenIsElevated;
        }
        catch (Exception e) when (e.HResult == Win32Error.ERROR_INVALID_PARAMETER)
        {
            return false;
        }
    }

    private static AdvApi32.SafeHTOKEN OpenProcessToken(IntPtr process)
    {
        if (!AdvApi32.OpenProcessToken(process, AdvApi32.TokenAccess.TOKEN_QUERY, out var handle))
            throw new Win32Exception();
        return handle;
    }
}