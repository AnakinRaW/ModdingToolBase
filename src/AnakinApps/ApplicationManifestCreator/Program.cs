using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.CommonUtilities.DownloadManager;
using AnakinRaW.CommonUtilities.Hashing;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testably.Abstractions;
#if DEBUG
using Microsoft.Extensions.Logging.Debug;
#endif

namespace AnakinRaW.ApplicationManifestCreator;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Console.WriteLine($"Raw Command line: {Environment.CommandLine}");
        return await Parser.Default.ParseArguments<ManifestCreatorOptions>(args)
            .MapResult(CreateManifest, ErrorArgs);
    }

    private static Task<int> ErrorArgs(IEnumerable<Error> arg)
    {
        return Task.FromResult(0xA0);
    }

    private static async Task<int> CreateManifest(ManifestCreatorOptions opts)
    {
        var services = CreateServices(opts);
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger(typeof(Program));
        try
        {
            var result = await new ManifestCreator(opts, services).Run();
            logger?.LogTrace($"ManifestCreator finished with result: {result}");
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

    private static IServiceProvider CreateServices(ManifestCreatorOptions options)
    {
        var services = new ServiceCollection();
        var fileSystem = new RealFileSystem();
        services.AddSingleton<IFileSystem>(fileSystem);
        services.AddSingleton<IHashingService>(sp => new HashingService(sp));

        services.AddUpdateFramework();

        services.AddSingleton(sp => new AppManifestCreatorBranchManager(options, sp));
        services.AddSingleton<IBranchManager>(sp => sp.GetRequiredService<AppManifestCreatorBranchManager>());

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