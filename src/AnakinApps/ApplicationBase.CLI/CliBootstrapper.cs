using System;
using System.IO.Abstractions;
using AnakinRaW.ApplicationBase.Options;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Interaction;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
#if DEBUG
using Microsoft.Extensions.Logging.Debug;
#endif


namespace AnakinRaW.ApplicationBase;

public abstract class CliBootstrapper : BootstrapperBase
{
    protected virtual bool AutomaticUpdate => false;

    protected abstract int ExecuteAfterUpdate(string[] args, IServiceCollection serviceCollection);

    private protected override void CreateCoreServices(IServiceCollection serviceCollection)
    {
        base.CreateCoreServices(serviceCollection);
        serviceCollection.Replace(ServiceDescriptor.Singleton<IUnhandledExceptionHandler>(sp => new ConsoleUnhandledExceptionHandler(sp)));
    }

    private protected override void CreateApplicationServices(IServiceCollection serviceCollection)
    {
        base.CreateApplicationServices(serviceCollection);
        serviceCollection.AddSingleton<IUpdateHandler>(sp => new UpdateHandler(sp));
        serviceCollection.AddSingleton<IUpdateResultInteractionHandler>(sp => new CommandLineResultInteractionHandler(sp));
        serviceCollection.AddSingleton<IUpdateOptionsProviderService>(sp => new UpdateOptionsProviderService());
        serviceCollection.AddSingleton<IUpdateOptionsProvider>(sp => sp.GetRequiredService<IUpdateOptionsProviderService>());
    }

    protected sealed override int Execute(string[] args, IServiceCollection serviceCollection)
    {
        var updateServices = serviceCollection.BuildServiceProvider();
        var updateResult = HandleUpdateApplication(args, updateServices, out var exitApplication);

        if (exitApplication || updateResult != 0)
            return updateResult;

        return ExecuteAfterUpdate(args, serviceCollection);
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

    private int HandleUpdateApplication(string[] args, IServiceProvider services, out bool exitApplication)
    {
        UpdaterCommandLineOptions? updateOptions = null;
        var wasUpdateCommand = false;

        new Parser(settings =>
            {
                settings.AutoHelp = false;
            }).ParseArguments<UpdateOption>(args)
            .WithParsed(options =>
            {
                updateOptions = options;
                wasUpdateCommand = true;
            }).WithNotParsed(_ =>
            {
                if (AutomaticUpdate && updateOptions is null)
                {
                    new Parser(settings =>
                        {
                            settings.AutoHelp = false;
                            settings.IgnoreUnknownArguments = true;
                        }).ParseArguments<UpdaterCommandLineOptions>(args)
                        .WithParsed(options => { updateOptions = options; })
                        .WithNotParsed(_ =>
                        {
                            updateOptions = new UpdateOption
                            {
                                AutomaticRestart = true
                            };
                        });
                }
            });

        exitApplication = wasUpdateCommand;

        if (updateOptions is null)
            return 0;

        var optionsProviderService = services.GetRequiredService<IUpdateOptionsProviderService>();
        optionsProviderService.SetOptions(updateOptions); 
        
        return new CommandLineToolSelfUpdater(services).UpdateIfNecessary(updateOptions);
    }
}