using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Debug;
using Testably.Abstractions;

namespace AnakinRaW.FtpUploader;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"Raw Command line: {Environment.CommandLine}");
        return await Parser.Default
            .ParseArguments<LocalUploadOptions, FtpUploadOptions>(args)
            .MapResult(
                (LocalUploadOptions opts) => UploadFiles(opts),
                (FtpUploadOptions opts) => UploadFiles(opts),
                ErrorArgs);
    }

    private static Task<int> ErrorArgs(IEnumerable<Error> arg)
    {
        return Task.FromResult(0xA0);
    }

    private static async Task<int> UploadFiles(UploadOptions opts)
    {
        var services = CreateServices();
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));
        try
        {
            UploaderBase uploader = opts.IsLocal
                ? new LocalUploader((LocalUploadOptions)opts, services)
                : new SftpUploader((FtpUploadOptions)opts, services) as UploaderBase;

            await using (uploader)
            {
                await uploader.Run();
            }

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

    private static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        var fileSystem = new RealFileSystem();
        services.AddSingleton<IFileSystem>(fileSystem);

        services.AddLogging(l =>
        {
            l.ClearProviders();

            // ReSharper disable once RedundantAssignment
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