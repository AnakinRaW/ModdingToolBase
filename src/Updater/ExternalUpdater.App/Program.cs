using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Tools;
using AnakinRaW.ExternalUpdater.Utilities;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Testably.Abstractions;
#if DEBUG
using Microsoft.Extensions.Logging.Debug;
#endif

namespace AnakinRaW.ExternalUpdater;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<ExternalRestartOptions, ExternalUpdateOptions>(args)
            .MapResult(
                (ExternalRestartOptions opts) => ExecuteApplication(opts),
                (ExternalUpdateOptions opts) => ExecuteApplication(opts),
                ErrorArgs);
    }

    private static async Task<int> ExecuteApplication(ExternalUpdaterOptions args)
    {
        var services = CreateServices(args);
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));

        logger?.LogTrace($"External updater started with commandline arguments: '{args.ToArgs()}'");

        try
        {
            var tool = new ToolFactory().Create(args, services);
            var result = await tool.Run();
            logger?.LogTrace($"Tool '{tool}' finished with result: {result}");
            return 0;
        }
        catch (Exception e)
        {
            logger?.LogCritical(e, e.Message);
#if DEBUG
            Console.WriteLine("Press enter to close!");
            Console.ReadLine();
#endif
            return e.HResult;
        }
    }

    private static Task<int> ErrorArgs(IEnumerable<Error> arg)
    {
#if DEBUG
        Console.WriteLine("Press enter to close!");
        Console.ReadLine();
#endif
        return Task.FromResult(0xA0);
    }

    private static IServiceProvider CreateServices(ExternalUpdaterOptions options)
    {
        var services = new ServiceCollection();
        var fileSystem = new RealFileSystem();
        services.AddSingleton<IFileSystem>(fileSystem);
        services.AddSingleton<IProcessTools>(sp => new ProcessTools(sp));

        services.AddLogging(l =>
        {
            l.ClearProviders();
#if DEBUG
            l.AddConsole().SetMinimumLevel(LogLevel.Trace);
            l.AddDebug().SetMinimumLevel(LogLevel.Trace);
#endif
            SetFileLogging(l);

        }).Configure<LoggerFilterOptions>(o =>
        {
#if DEBUG
            o.AddFilter<DebugLoggerProvider>(null, LogLevel.Trace);
#endif
            o.AddFilter<SerilogLoggerProvider>(null, LogLevel.Trace);
        });

        return services.BuildServiceProvider();


        void SetFileLogging(ILoggingBuilder builder)
        {
            const string logFile = "extUpdateLog.txt";
            var logPath = logFile;

            var logDir = options.LoggingDirectory;
            if (!string.IsNullOrEmpty(logDir))
            {
                fileSystem.Directory.CreateDirectory(logDir!);
                logPath = fileSystem.Path.Combine(logDir!, logPath);
            }
            builder.AddFile(logPath, LogLevel.Trace, 
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}");
        }
    }
}