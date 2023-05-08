﻿using System.IO.Abstractions;
using System.Threading.Tasks;
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
using System;
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
        var updateOptions = GetUpdateOptionsFromCommandLine(args, out var wasExplicitUpdate);

        if (updateOptions is not null)
        {
            var updateServices = serviceCollection.BuildServiceProvider();

            Task.Run(async () => await updateServices.GetRequiredService<IExternalUpdateExtractor>().ExtractAsync().ConfigureAwait(false)).Wait();

            var optionsProviderService = updateServices.GetRequiredService<IUpdateOptionsProviderService>(); 
            optionsProviderService.SetOptions(updateOptions);
            
            var updateResult = new CommandLineToolSelfUpdater(updateServices).UpdateIfNecessary(updateOptions);

            if (wasExplicitUpdate || updateResult != 0)
            {
#if DEBUG
                Console.WriteLine("Press Enter to exit.");
                Console.ReadLine();
#endif
                return updateResult;
            }
        }

        return ExecuteAfterUpdate(args, serviceCollection);
    }

    protected override void SetupLogging(IServiceCollection serviceCollection, IFileSystem fileSystem,
        IApplicationEnvironment applicationEnvironment)
    {
        base.SetupLogging(serviceCollection, fileSystem, applicationEnvironment);

        serviceCollection.AddLogging(l =>
        {
            l.ClearProviders();

            var appVersion = applicationEnvironment.AssemblyInfo.InformationalAsSemVer();
            
            // ReSharper disable once RedundantAssignment
            var logLevel = LogLevel.Information;

            if (appVersion is not null && appVersion.IsPrerelease)
                logLevel = LogLevel.Debug;

            
            var logPath = fileSystem.Path.Combine(applicationEnvironment.ApplicationLocalDirectory.FullName, "log");
            l.AddFile(logPath, logLevel);

#if DEBUG
            logLevel = LogLevel.Trace;
            l.AddConsole().SetMinimumLevel(logLevel);
            l.AddDebug().SetMinimumLevel(logLevel);
#endif

        }).Configure<LoggerFilterOptions>(o =>
        {
#if DEBUG
            o.AddFilter<DebugLoggerProvider>(null, LogLevel.Trace);
#endif
        });
    }


    internal IUpdaterCommandLineOptions? GetUpdateOptionsFromCommandLine(string[] args, out bool wasExplicitUpdate)
    {
        UpdaterCommandLineOptions? updateOptions = null;
        var wasUpdateCommand = false;

        new Parser(settings =>
            {
                settings.AutoHelp = false;
                settings.IgnoreUnknownArguments = false;
                settings.GetoptMode = true;
            })
            // Not sure why, but apparently we need to use at least the T2 generic method. Otherwise,
            // we might always parse ExplicitUpdateOption
            .ParseArguments<ExplicitUpdateOption, UpdaterCommandLineOptions>(args)
            .WithParsed<ExplicitUpdateOption>(options =>
            {
                updateOptions = options;
                wasUpdateCommand = true;
            }).WithNotParsed(_ =>
            {
                if (AutomaticUpdate)
                {
                    new Parser(settings =>
                        {
                            settings.AutoHelp = false;
                            settings.IgnoreUnknownArguments = true;
                        }).ParseArguments<UpdaterCommandLineOptions>(args)
                        .WithParsed(options =>
                        {
                            updateOptions = options;
                        })
                        .WithNotParsed(_ =>
                        {
                            updateOptions = new ExplicitUpdateOption
                            {
                                AutomaticRestart = true
                            };
                        });
                }
            });

        wasExplicitUpdate = wasUpdateCommand;
        return updateOptions;
    }
}