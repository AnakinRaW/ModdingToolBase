using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.CommonUtilities.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace AnakinRaW.ApplicationBase;

public abstract class SelfUpdateableAppLifecycle
{
    private IServiceProvider _bootstrapperServices = null!;

    private string? _bootstrapperLoggingDir;

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
        StartInternal(args);
        var appServices = CreateAppServices(); 
        // ConfigureAwait cannot be set to false here, because WPF apps might expect the context of the main thread.
        return await RunAppAsync(args, appServices);
    }

    private IServiceProvider CreateAppServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(FileSystem);
        services.AddSingleton(Registry);
        services.AddSingleton(ApplicationEnvironment);

        if (ApplicationEnvironment is UpdatableApplicationEnvironment updatableApplication)
            services.AddSingleton<IUpdateConfigurationProvider>(updatableApplication);

        CreateAppServices(services);
        return services.BuildServiceProvider();
    }

    protected abstract ApplicationEnvironment CreateAppEnvironment();

    protected abstract IFileSystem CreateFileSystem();

    protected abstract IRegistry CreateRegistry();

    protected abstract Task<int> RunAppAsync(string[] args, IServiceProvider appServiceProvider);

    protected abstract void ResetApp();

    protected virtual void CreateAppServices(IServiceCollection services)
    {
    }

    protected virtual void InitializeApp()
    {
        if (!FileSystem.Directory.Exists(ApplicationEnvironment.ApplicationLocalPath)) 
            ApplicationEnvironment.ApplicationLocalDirectory.Create();
    }

    private void StartInternal(string[] args)
    {
        _bootstrapperServices = CreateBootstrapperServices();

        if (ApplicationEnvironment is UpdatableApplicationEnvironment updatableApplicationEnvironment)
        {
            using var updateBootstrapper = new SelfUpdatableAppBootstrapper(
                updatableApplicationEnvironment,
                _bootstrapperServices,
                _bootstrapperLoggingDir);
            var selfUpdateResult = updateBootstrapper.HandleSelfUpdate(args);

            if (selfUpdateResult == SelfUpdateResult.Reset)
                ResetApp();
            if (selfUpdateResult == SelfUpdateResult.RestartRequired)
                System.Environment.Exit(RestartConstants.RestartRequiredCode);
        }

        // Initialization of the app must happen after completing the self-update process.
        InitializeApp();
    }

    private IServiceProvider CreateBootstrapperServices()
    {
        var serviceCollection = new ServiceCollection();
        
        serviceCollection.AddSingleton(FileSystem);
        serviceCollection.AddSingleton(Registry);
        serviceCollection.AddSingleton(ApplicationEnvironment);

        CreateBootstrapLogger(serviceCollection);

        return serviceCollection.BuildServiceProvider();
    }

    private void CreateBootstrapLogger(IServiceCollection serviceCollection)
    {
        serviceCollection.AddLogging(c =>
        {
            c.ClearProviders();

            var minLogLevel =
#if DEBUG
                LogEventLevel.Verbose;
#else
                LogEventLevel.Debug;
#endif

            var fileSystem = FileSystem;

            var tempDir = fileSystem.Path.GetTempPath();
            var tempSubFolderName = fileSystem.Path.GetRandomFileName();

            var loggingDir = _bootstrapperLoggingDir = fileSystem.Path.GetFullPath(fileSystem.Path.Combine(tempDir, tempSubFolderName));

            var filePath = (FileSystem.Path.Combine(loggingDir, "appBootstrap.log"));

            var fileLogger = new LoggerConfiguration()
                .WriteTo.File(filePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: minLogLevel)
                .CreateLogger();

            c.AddSerilog(fileLogger);
        });
    }
}