using System;
using System.Diagnostics;
using Vanara.PInvoke;


namespace AnakinRaW.ApplicationBase.Utilities;

internal class CurrentProcessInfo
{
    public static readonly CurrentProcessInfo Current = new();

    public string ProcessFilePath;

    public int Id;

    private CurrentProcessInfo()
    {
        var p = Process.GetCurrentProcess();
        Id = p.Id;
#if NET6_0
        var processPath = Environment.ProcessPath;
#else
        var processPath = Kernel32.GetModuleFileName(HINSTANCE.NULL);
#endif
        if (string.IsNullOrEmpty(processPath))
            throw new InvalidOperationException("Unable to get current process path");
        ProcessFilePath = processPath;
    }
}