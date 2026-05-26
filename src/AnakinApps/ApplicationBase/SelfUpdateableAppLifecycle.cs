using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Options;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.CommonUtilities.Registry;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AnakinRaW.ApplicationBase;

public abstract class SelfUpdateableAppLifecycle
{
    private IServiceProvider _bootstrapperServices = null!;

    protected ILoggerFactory? BootstrapLoggerFactory { get; private set; }

    protected ILogger? Logger { get; private set; }

    [MemberNotNullWhen(true, nameof(UpdatableApplicationEnvironment))]
    protected bool IsUpdateableApplication => UpdatableApplicationEnvironment is not null;

    protected ApplicationEnvironment ApplicationEnvironment
    {
        get => LazyInitializer.EnsureInitialized(ref field, CreateAppEnvironment);
    } = null!;

    protected UpdatableApplicationEnvironment? UpdatableApplicationEnvironment =>
        ApplicationEnvironment as UpdatableApplicationEnvironment;

    protected IFileSystem FileSystem
    {
        get => LazyInitializer.EnsureInitialized(ref field, CreateFileSystem);
    } = null!;

    protected IRegistry Registry
    {
        get => LazyInitializer.EnsureInitialized(ref field, CreateRegistry);
    } = null!;

    public int Start(string[] args)
    {
        return StartAsync(args).GetAwaiter().GetResult();
    }

    public async Task<int> StartAsync(string[] args)
    {
        _bootstrapperServices = CreateBootstrapperServices(args);

        BootstrapLoggerFactory = _bootstrapperServices.GetService<ILoggerFactory>();
        var logger = BootstrapLoggerFactory?.CreateLogger(GetType());
        logger?.LogTrace("Application started with raw arguments: '{Args}'", System.Environment.CommandLine);
        Logger = logger;

        var resetRequested = ShouldPreBootstrapResetApp(args);
        if (resetRequested)
        {
            logger?.LogWarning("The application was requested to reset itself.");
            ResetApp();
        }

        logger?.LogInformation("Initializing application.");
        var initResult = await InitializeAppAsync(args, _bootstrapperServices);
        if (initResult != 0)
        {
            logger?.LogWarning("Initialization was not successful, error code: {Result}. Terminating application.", initResult);
            return initResult;
        }

        logger?.LogInformation("Creating app services.");
        var appServices = CreateAppServices(args);

        if (IsUpdateableApplication)
            appServices.GetRequiredService<IExternalUpdaterProvider>().EnsureAvailable();

        // Trust must be populated before the resume path verifies any persisted manifest.
        RegisterTrustedCertificates(appServices);

        if (!resetRequested)
        {
            // There is no reason to continue a pending update if we reset the application before.
            await HandlePendingUpdateAsync(args, appServices).ConfigureAwait(false);
        }

        // ConfigureAwait cannot be set to false here, because WPF apps might expect the context of the main thread.
        return await RunAppAsync(args, appServices);
    }

    private IServiceProvider CreateAppServices(IReadOnlyList<string> args)
    {
        var services = new ServiceCollection();

        services.AddSingleton(FileSystem);
        services.AddSingleton(Registry);
        services.AddSingleton(ApplicationEnvironment);

        if (ApplicationEnvironment is UpdatableApplicationEnvironment updatableApplication)
            services.AddSingleton<IUpdateConfigurationProvider>(updatableApplication);

        CreateAppServices(services, args);
        return services.BuildServiceProvider();
    }

    protected abstract ApplicationEnvironment CreateAppEnvironment();

    protected abstract IFileSystem CreateFileSystem();

    protected abstract IRegistry CreateRegistry();

    protected abstract Task<int> RunAppAsync(string[] args, IServiceProvider appServiceProvider);

    protected virtual void ResetApp()
    {
        var updateEnv = UpdatableApplicationEnvironment;
        if (updateEnv is null)
            return;
        using var updateRegistry = new ApplicationUpdateRegistry(Registry, updateEnv);
        updateRegistry.Reset();
        _bootstrapperServices.GetRequiredService<IPendingUpdate>().Clear();
    }

    protected virtual void CreateAppServices(IServiceCollection services, IReadOnlyList<string> args)
    {
    }

    /// <summary>
    /// Populates the framework's trust store before the deferred-update resume path verifies the persisted manifest.
    /// </summary>
    protected virtual void RegisterTrustedCertificates(IServiceProvider appServices)
    {
    }
    
    protected virtual Task<int> InitializeAppAsync(IReadOnlyList<string> args, IServiceProvider bootstrapServices)
    {
        if (!FileSystem.Directory.Exists(ApplicationEnvironment.ApplicationLocalPath)) 
            ApplicationEnvironment.ApplicationLocalDirectory.Create();
        return Task.FromResult(0);
    }

    protected virtual bool ShouldPreBootstrapResetApp(IReadOnlyList<string> args)
    {
        return false;
    }

    private async Task HandlePendingUpdateAsync(string[] args, IServiceProvider appServices)
    {
        if (ApplicationEnvironment is UpdatableApplicationEnvironment updatableApplicationEnvironment)
        {
            Logger?.LogInformation($"App environment is of type '{nameof(Environment.UpdatableApplicationEnvironment)}'. Executing update finalization routine.");

            using var updateBootstrapper = new SelfUpdateBootstrapper(
                updatableApplicationEnvironment,
                appServices);
            var selfUpdateResult = await updateBootstrapper.UpdateAsync(args).ConfigureAwait(false);

            if (selfUpdateResult == SelfUpdateResult.Reset)
            {
                Logger?.LogWarning("Self update failed ungracefully. Resetting application...");
                ResetApp();
            }
        }
    }

    private ServiceProvider CreateBootstrapperServices(IReadOnlyList<string> args)
    {
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton(FileSystem);
        serviceCollection.AddSingleton(Registry);
        serviceCollection.AddSingleton(ApplicationEnvironment);

        if (ApplicationEnvironment is UpdatableApplicationEnvironment updatableApplication)
        {
            serviceCollection.AddSingleton<IUpdateConfigurationProvider>(updatableApplication);
            serviceCollection.AddPendingUpdate();
        }

        var verboseLogging = false;

        using var parser = new Parser(s =>
        {
            s.IgnoreUnknownArguments = true;
        });
        parser.ParseArguments<VerboseLoggingOption>(args).WithParsed(verboseOptions => verboseLogging = verboseOptions.VerboseBootstrapLogging);
        CreateBootstrapLogger(serviceCollection, verboseLogging);

        return serviceCollection.BuildServiceProvider();
    }

    private void CreateBootstrapLogger(IServiceCollection serviceCollection, bool verboseLogging)
    {
        serviceCollection.AddLogging(c =>
        {
            c.ClearProviders();

            // ReSharper disable once RedundantAssignment
            var logLevel = LogEventLevel.Information;
#if DEBUG
            logLevel = LogEventLevel.Debug;
            c.AddDebug();
#endif

            if (verboseLogging) 
                logLevel = LogEventLevel.Verbose;
            
            var fileSystem = FileSystem;

            var tempDir = fileSystem.Path.GetTempPath();
            
            var encodedAppName = EncodeDirectoryName(ApplicationEnvironment.ApplicationName);

            var loggingDir = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(tempDir, encodedAppName));

            var filePath = FileSystem.Path.Combine(loggingDir, $"{encodedAppName}-Bootstrap.log");

            var fileLogger = new LoggerConfiguration()
                .WriteTo.File(filePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: logLevel)
                .MinimumLevel.Is(logLevel)
                .CreateLogger();

            c.AddSerilog(fileLogger);
        });
    }

    private string EncodeDirectoryName(string appName)
    {
        // Using GetInvalidFileNameChars() instead of GetInvalidPathChars in order to encode path control characters ('/' or '\\').
        var invalidChars = Regex.Escape(new string(FileSystem.Path.GetInvalidFileNameChars()));
        var pattern = $"[{invalidChars}]";
        return Regex.Replace(appName, pattern, "_");
    }
}