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
        string? passThroughArgs,
        bool elevate)
    {
        if (!application.Exists)
            throw new FileNotFoundException("The executable was not found.", application.FullName);

        var startInfo = new ProcessStartInfo(application.FullName)
        {
            UseShellExecute = true,
        };

        AddArgumentsToStartInfo(startInfo, appStartOptions, passThroughArgs);

        if (elevate)
            startInfo.Verb = "runas";
        using var process = new Process();
        process.StartInfo = startInfo;
        _logger?.LogInformation($"Starting application '{startInfo.FileName}' with {startInfo.Arguments}");
        process.Start();
    }


    private static void AddArgumentsToStartInfo(
        ProcessStartInfo startInfo, 
        ExternalUpdaterResultOptions resultOptions,
        string? passThroughArguments)
    {
#if NET
        var resultArgs = Parser.Default.FormatCommandLineArgs(resultOptions);
        
        string[]? passThroughArgList = !string.IsNullOrEmpty(passThroughArguments)
            // We don't use keepQuote=true here, because starting a process with ArgumentList already handles escaping.
            ? passThroughArguments.SplitArgs() 
            : [];

        foreach (var arg in resultArgs.Concat(passThroughArgList)) 
            startInfo.ArgumentList.Add(arg);  
#else
        var sb = new StringBuilder();

        sb.Append(Parser.Default.FormatCommandLine(resultOptions));

        if (!string.IsNullOrEmpty(passThroughArguments))
        {
            sb.Append(' ');
            sb.Append(passThroughArguments);
        }

        startInfo.Arguments = sb.ToString();
#endif
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
                    _logger?.LogDebug($"Waiting for {parentProcess.ProcessName} to exit...");
                    await parentProcess.WaitForExitAsync(token);
                }
                catch (TaskCanceledException)
                {
                    if (parentProcess.HasExited)
                        return true;

                    _logger?.LogError($"The process '{parentProcess.ProcessName}:{parentProcess.Id}' did not exit before timeout was reached. Aborting...");
                    return false;
                }
                catch (Exception e)
                {
                    if (parentProcess.HasExited)
                        return true;

                    _logger?.LogCritical(e, $"Unable to wait for process '{parentProcess.ProcessName}:{parentProcess.Id}' to terminate: {e.Message}");
                    return false;
                }
            }
        }
        return true;
    }
}