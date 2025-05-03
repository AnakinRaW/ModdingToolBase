using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Options;

namespace AnakinRaW.ExternalUpdater.Tools;

internal abstract class ProcessTool<T>(T options, IServiceProvider serviceProvider)
    : ToolBase<T>(options, serviceProvider)
    where T : ExternalUpdaterOptions
{
    protected async Task WaitForProcessExitAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Options.Timeout));
        if (!await ProcessTools.WaitForExitAsync(Options.Pid, cts.Token))
            throw new InvalidOperationException($"Application with '{Options.Pid}' was not closed");
    }

    protected void StartProcess(ExternalUpdaterResult operationResult)
    {
        var processToStart = FileSystem.FileInfo.New(Options.AppToStart);
        if (!processToStart.Exists)
            throw new FileNotFoundException("Could not find application to restart.", processToStart.FullName);
        
        var options = new ExternalUpdaterResultOptions { Result = operationResult };
        ProcessTools.StartApplication(processToStart, options, Options.AppToStartArguments, Options.Elevate);
    }
}