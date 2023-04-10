using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.CommonUtilities.Hashing;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

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
            logger?.LogCritical(e, e.Message);
            return e.HResult;
        }
    }

    private static IServiceProvider CreateServices(ManifestCreatorOptions options)
    {
        var services = new ServiceCollection();
        var fileSystem = new FileSystem();
        services.AddSingleton<IFileSystem>(fileSystem);
        services.AddSingleton<IMetadataExtractor>(sp => new MetadataExtractor(sp));
        services.AddSingleton<IHashingService>(_ => new HashingService());

        var bm = new AppCreatorBranchManager(options);
        services.AddSingleton<IBranchManager>(bm);
        services.AddSingleton(bm);

        services.AddLogging(l =>
        {
            l.ClearProviders();
#if DEBUG
            l.AddConsole().SetMinimumLevel(LogLevel.Trace);
            l.AddDebug().SetMinimumLevel(LogLevel.Trace);
#endif
        }).Configure<LoggerFilterOptions>(o =>
        {
#if DEBUG
            o.AddFilter<DebugLoggerProvider>(null, LogLevel.Trace);
#endif
        });

        return services.BuildServiceProvider();
    }
}