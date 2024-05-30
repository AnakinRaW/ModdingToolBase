using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace AnakinRaW.FtpUploader;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"Raw Command line: {Environment.CommandLine}");
        return await Parser.Default.ParseArguments<FtpUploadOptions>(args)
            .MapResult(UploadFiles, ErrorArgs);
    }

    private static Task<int> ErrorArgs(IEnumerable<Error> arg)
    {
        return Task.FromResult(0xA0);
    }

    private static async Task<int> UploadFiles(FtpUploadOptions opts)
    {
        var services = CreateServices(opts);
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));
        try
        {
            await using var uploader = new Uploader(opts, services);
            await uploader.Run();
            logger?.LogTrace("Uploader finished");
            return 0;
        }
        catch (Exception e)
        {
            return await Task.Run(() =>
            {
                logger?.LogCritical(e, e.Message);
                return e.HResult;
            });
        }
    }

    private static IServiceProvider CreateServices(FtpUploadOptions options)
    {
        var services = new ServiceCollection();
        var fileSystem = new FileSystem();
        services.AddSingleton<IFileSystem>(fileSystem);

        services.AddLogging(l =>
        {
            l.ClearProviders();

            var logLevel = LogLevel.Information;
#if DEBUG
            logLevel = LogLevel.Trace;
            l.AddDebug().SetMinimumLevel(logLevel);
#endif
            l.AddConsole().SetMinimumLevel(logLevel);
        }).Configure<LoggerFilterOptions>(o =>
        {
#if DEBUG
            o.AddFilter<DebugLoggerProvider>(null, LogLevel.Trace);
#endif
        });

        return services.BuildServiceProvider();
    }
}