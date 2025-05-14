using System.Collections.Generic;
using System.Linq;
using Vanara.PInvoke;
using Process = System.Diagnostics.Process;
#if DEBUG
using System.Runtime.InteropServices;
using Debugger = System.Diagnostics.Debugger;
using Microsoft.VisualStudio.OLE.Interop;
using EnvDTE;
#endif

namespace AnakinRaW.AppUpdaterFramework.FileLocking;

internal static class Utilities
{
    private static readonly Process CurrentProcess = Process.GetCurrentProcess();

    public static bool ContainsCurrentProcess(this IEnumerable<ILockingProcessInfo> processes)
    {
        return processes.Any(x => x.IsCurrentProcess());
    }

    public static IEnumerable<ILockingProcessInfo> WithoutDebugger(this IEnumerable<ILockingProcessInfo> processes)
    {
#if DEBUG
        if (!Debugger.IsAttached)
            return processes;
        var debugger = GetDebuggerProcess();
        return debugger is null ? processes : processes.Where(x => x.Id != debugger);
#else
        return processes;
#endif
    }

    public static IEnumerable<ILockingProcessInfo> WithoutCurrentProcess(this IEnumerable<ILockingProcessInfo> processes)
    {
        return processes.Where(x => !x.IsCurrentProcess());
    }

    public static IEnumerable<ILockingProcessInfo> WithoutStopped(this IEnumerable<ILockingProcessInfo> processes)
    {
        return processes.Where(x => !x.IsStopped());
    }

    public static bool AllStopped(this IEnumerable<ILockingProcessInfo> processes)
    { 
        return processes.All(x => x.IsStopped());
    }

    public static bool AnyRunning(this IEnumerable<ILockingProcessInfo> processes)
    {
        return processes.Any(x => x.ApplicationStatus is RstrtMgr.RM_APP_STATUS.RmStatusUnknown or RstrtMgr.RM_APP_STATUS.RmStatusRunning or RstrtMgr.RM_APP_STATUS.RmStatusRestarted);
    }

    public static bool IsCurrentProcess(this ILockingProcessInfo process)
    {
        return process.Id == CurrentProcess.Id;
    }

    public static bool IsStopped(this ILockingProcessInfo process)
    {
        return process.ApplicationStatus is RstrtMgr.RM_APP_STATUS.RmStatusStopped
            or RstrtMgr.RM_APP_STATUS.RmStatusStoppedOther;
    }

#if DEBUG

    private static int? GetDebuggerProcess()
    {
        foreach (var instance in GetVsInstances())
        {
            var processes = instance.Vs.Debugger.DebuggedProcesses;
            if (processes.Cast<EnvDTE.Process>().Any(process => process.ProcessID == CurrentProcess.Id))
                return instance.Pid;
        }

        return null;
    }

    [DllImport("ole32.dll")]
    public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

    [DllImport("ole32.dll")]
    public static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

    private static IEnumerable<(_DTE Vs, int Pid)> GetVsInstances()
    {
        var monikers = new IMoniker[1];

        GetRunningObjectTable(0, out var runningObjectTable);
        runningObjectTable.EnumRunning(out var monikerEnumerator);
        monikerEnumerator.Reset();

        while (monikerEnumerator.Next(1, monikers, out _) == 0)
        {
            CreateBindCtx(0, out var ctx);
            var moniker = monikers[0];

            try
            {
                monikers[0].GetDisplayName(ctx, null, out var runningObjectName);
                Marshal.ReleaseComObject(ctx);

                runningObjectTable.GetObject(monikers[0], out var runningObjectVal);

                if (runningObjectVal is _DTE dte && runningObjectName.StartsWith("!VisualStudio.DTE"))
                {
                    var currentProcessId = int.Parse(runningObjectName.Split(':')[1]);
                    yield return (dte, currentProcessId);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(moniker);
            }
        }
    }
#endif
}