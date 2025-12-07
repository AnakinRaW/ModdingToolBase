using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if NETFRAMEWORK
using System.Text;
using AnakinRaW.CommonUtilities;
#endif

namespace AnakinRaW.ExternalUpdater.Utilities;

internal class ProcessTools : IProcessTools
{
    private readonly ILogger? _logger;

    public ProcessTools(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public void StartApplication(
        IFileInfo application,
        ExternalUpdaterResultOptions appStartOptions,
        string? passThroughArgsBase64,
        bool elevate)
    {
        if (!application.Exists)
            throw new FileNotFoundException("The executable was not found.", application.FullName);

        var startInfo = new ProcessStartInfo(application.FullName)
        {
            UseShellExecute = true,
        };

        AddArgumentsToStartInfo(startInfo, appStartOptions, passThroughArgsBase64);

        if (elevate)
            startInfo.Verb = "runas";
        using var process = new Process();
        process.StartInfo = startInfo;
        _logger?.LogInformation("Starting application '{Name}' with {Args}", startInfo.FileName, startInfo.Arguments);
        process.Start();
    }


    private static void AddArgumentsToStartInfo(
        ProcessStartInfo startInfo, 
        ExternalUpdaterResultOptions resultOptions,
        string? passThroughArgsBase64)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(passThroughArgsBase64))
        {
            var decoded = Encoding.Default.GetString(Convert.FromBase64String(passThroughArgsBase64));
            sb.Append(decoded);
            sb.Append(' ');
        }

        // We need to append (not prepend) in order not to break verbs which may be present in passThroughArgsBase64
        sb.Append(Parser.Default.FormatCommandLine(resultOptions));

        startInfo.Arguments = sb.ToString();
    }


    public async Task<bool> WaitForExitAsync(int? pid, CancellationToken token)
    {
        if (pid.HasValue)
        {
            var parentProcess = Process.GetProcesses().FirstOrDefault(x => x.Id == pid.Value);
            if (parentProcess != null)
            {
                try
                {
                    _logger?.LogDebug("Waiting for {Process} to exit...", parentProcess.ProcessName);
                    await parentProcess.WaitForExitAsync(token);
                }
                catch (TaskCanceledException)
                {
                    if (parentProcess.HasExited)
                        return true;

                    _logger?.LogError("The process '{Process}:{Pid}' did not exit before timeout was reached. Aborting...", parentProcess.ProcessName, parentProcess.Id);
                    return false;
                }
                catch (Exception e)
                {
                    if (parentProcess.HasExited)
                        return true;

                    _logger?.LogCritical(e, "Unable to wait for process '{Process}:{Pid}' to terminate: {Message}", parentProcess.ProcessName, parentProcess.Id, e.Message);
                    return false;
                }
            }
        }
        return true;
    }
}