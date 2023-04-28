using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Interaction;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
#if DEBUG
using Microsoft.Extensions.Logging.Debug;
#endif


namespace AnakinRaW.ApplicationBase;

public abstract class CliBootstrapper : BootstrapperBase
{
    protected virtual bool AutomaticUpdate => false;

    protected sealed override int Execute(string[] args, IServiceCollection serviceCollection)
    {
        if (AutomaticUpdate)
        {
            //updaterOptions = new UpdateOptions
            //{
            //    AutomaticRestart = true
            //};
        }
        else
        {
            var shallExit = false;
            var parser = new Parser(settings => settings.AutoHelp = false);
            parser.ParseArguments<UpdateOptions>(args).WithParsed(options =>
            {
                throw new AbandonedMutexException();
                using var updateServiceProvider = serviceCollection.BuildServiceProvider();
                new CommandLineToolSelfUpdater(options, updateServiceProvider).UpdateIfNecessary();
                shallExit = true;
            });

            if (shallExit)
                return 0;
        }

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
            l.AddConsole().SetMinimumLevel(LogLevel.Information);
        }).Configure<LoggerFilterOptions>(o =>
        {
#if DEBUG
            o.AddFilter<DebugLoggerProvider>(null, LogLevel.Trace);
#endif
        });
    }
}