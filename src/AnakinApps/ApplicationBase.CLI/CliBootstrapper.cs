using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Options;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Events;

#if DEBUG
using System;
#endif


namespace AnakinRaW.ApplicationBase;

public abstract class CliBootstrapper : BootstrapperBase
{
    protected virtual bool AutomaticUpdate => false;

    protected virtual IEnumerable<string>? AdditionalNamespacesToLogToConsole => null;

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
        serviceCollection.AddSingleton<IUpdateOptionsProviderService>(_ => new UpdateOptionsProviderService());
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

    protected override void ConfigureLogging(ILoggingBuilder loggingBuilder, IFileSystem fileSystem,
        IApplicationEnvironment applicationEnvironment)
    {
        loggingBuilder.ClearProviders();

        var appVersion = applicationEnvironment.AssemblyInfo.InformationalAsSemVer();

        // ReSharper disable once RedundantAssignment
        var logLevel = LogLevel.Information;

        if (appVersion is not null && appVersion.IsPrerelease)
        {
            // ReSharper disable once RedundantAssignment
            logLevel = LogLevel.Debug;
        }

#if DEBUG
        loggingBuilder.AddDebug();
#endif
        loggingBuilder.AddConsole();

        var logPath = fileSystem.Path.Combine(applicationEnvironment.ApplicationLocalDirectory.FullName, "log.txt");

        SetupFileLogging(loggingBuilder, logPath);

        loggingBuilder.AddFilter<ConsoleLoggerProvider>((category, level) =>
        {
            if (level < logLevel)
                return false;
            if (string.IsNullOrEmpty(category)) 
                return false;

            if (category!.StartsWith(GetType().Namespace!) || category.StartsWith(nameof(ApplicationBase)))
                return true;

            if (AdditionalNamespacesToLogToConsole is null)
                return false;

            foreach (var @namespace in AdditionalNamespacesToLogToConsole)
            {
                if (category.StartsWith(@namespace))
                    return true;
            }
            
            return false;
        });
    }

    private void SetupFileLogging(ILoggingBuilder loggingBuilder, string logFilePath)
    {
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Verbose()
            .Filter.ByExcluding(ExcludeFromGlobalLogging)
            .WriteTo.RollingFile(
                logFilePath, 
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}")
            .CreateLogger();

        loggingBuilder.AddSerilog(logger);
    }

    protected virtual bool ExcludeFromGlobalLogging(LogEvent logEvent)
    {
        return false;
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