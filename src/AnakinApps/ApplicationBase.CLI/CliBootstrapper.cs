using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Interaction;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

namespace AnakinRaW.ApplicationBase;

public abstract class CliBootstrapper : BootstrapperBase
{
    protected sealed override int Execute(string[] args, IServiceCollection serviceCollection)
    {
        // By default we update the application using the current branch
        var updaterOptions = UpdaterCommandLineOptions.Default;

        var parser = new Parser(settings => settings.IgnoreUnknownArguments = true);
        parser.ParseArguments<UpdaterCommandLineOptions>(args).WithParsed(options => updaterOptions = options);

        using var updateServiceProvider = serviceCollection.BuildServiceProvider();
        new CommandLineToolSelfUpdater(updaterOptions, updateServiceProvider).UpdateIfNecessary();

        return ExecuteAfterUpdate(args, serviceCollection);
    }

    protected abstract int ExecuteAfterUpdate(string[] args, IServiceCollection serviceCollection);

    private protected override void CreateApplicationServices(IServiceCollection serviceCollection)
    {
        base.CreateApplicationServices(serviceCollection);

        serviceCollection.AddSingleton<IUpdateHandler>(sp => new UpdateHandler(sp));
        serviceCollection.AddSingleton<IUpdateResultInteractionHandler>(sp => new CommandLineResultInteractionHandler(sp));
    }

    protected override void SetupLogging(IServiceCollection serviceCollection, IFileSystem fileSystem,
        IApplicationEnvironment applicationEnvironment)
    {
        base.SetupLogging(serviceCollection, fileSystem, applicationEnvironment);

        serviceCollection.AddLogging(l =>
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
    }
}