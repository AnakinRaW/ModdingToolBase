using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

public class CurrentProcessInfo
{
    public static readonly CurrentProcessInfo Current = new();

    public readonly string ProcessFilePath;

    public readonly int Id;

    private CurrentProcessInfo()
    {
        var p = Process.GetCurrentProcess();
        Id = p.Id;
#if NET6_0
        var processPath = Environment.ProcessPath;
#else
        var processPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Kernel32.GetModuleFileName(HINSTANCE.NULL)
            : Process.GetCurrentProcess().MainModule.FileName;
#endif
        if (string.IsNullOrEmpty(processPath))
            throw new InvalidOperationException("Unable to get current process path");
        ProcessFilePath = processPath!;
    }
}