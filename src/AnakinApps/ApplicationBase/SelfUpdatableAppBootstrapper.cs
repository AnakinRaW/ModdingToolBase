using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.ExternalUpdater;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AnakinRaW.ApplicationBase;

public enum SelfUpdateResult
{
    None,
    Success,
    Reset,
    RestartRequired
}

public sealed class SelfUpdatableAppBootstrapper : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string? _logFileDirectory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly UpdatableApplicationEnvironment _applicationEnvironment;
    private readonly ApplicationUpdateRegistry _updateRegistry;

    public SelfUpdatableAppBootstrapper(
        UpdatableApplicationEnvironment applicationEnvironment, 
        IServiceProvider services,
        string? logFileDirectory = null)
    {
        _applicationEnvironment = applicationEnvironment;
        _serviceProvider = services;
        _logFileDirectory = logFileDirectory;
        _fileSystem = services.GetRequiredService<IFileSystem>();
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        
        var registry = services.GetRequiredService<IRegistry>();
        _updateRegistry = new ApplicationUpdateRegistry(registry, _applicationEnvironment);
    }

    public SelfUpdateResult HandleSelfUpdate(string[] args)
    {
        if (!_applicationEnvironment.UpdateConfiguration.RestartConfiguration.SupportsRestart)
            return SelfUpdateResult.None;

        if (ExternalUpdaterResultOptions.TryParse(args, out var externalUpdaterResult) && !HandleRestartResult(externalUpdaterResult.Result))
            return SelfUpdateResult.Reset;

        if (_updateRegistry.RequiresUpdate)
        {
            _logger?.LogInformation("Registry indicating update is required: Running external updater...");
            try
            {
                LaunchExternalUpdater();
                _logger?.LogInformation("ExternalUpdater running. Closing application!");
                return SelfUpdateResult.RestartRequired;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Failed to run ExternalUpdater. Starting application normally: {e.Message}");
                _updateRegistry.Reset();
            }
        }

        return SelfUpdateResult.Success;
    }

    public void Dispose()
    {
        _updateRegistry.Dispose();
    }

    private bool HandleRestartResult(ExternalUpdaterResult result)
    {
        _logger?.LogTrace($"ExternalUpdater result: '{result}'");

        if (result == ExternalUpdaterResult.UpdateFailedNoRestore || _updateRegistry.ResetApp)
        {
            _logger?.LogDebug($"Resetting app due to ExternalUpdater result '{result}' or UpdateRegistry/ResetApp = {_updateRegistry.ResetApp}");
            _updateRegistry.Reset();
            return false;
        }

        if (result is ExternalUpdaterResult.UpdateFailedWithRestore or ExternalUpdaterResult.UpdateSuccess)
        {
            _logger?.LogDebug($"ExternalUpdater indicated result '{result}'.");
            _updateRegistry.Reset();
        }

        return true;
    }

    private void LaunchExternalUpdater()
    {
        var updaterPath = _updateRegistry.UpdaterPath;
        if (string.IsNullOrEmpty(updaterPath))
            throw new InvalidOperationException("No path for ExternalUpdater set in registry.");

        var updater = _fileSystem.FileInfo.New(updaterPath!);

        var updateArgs = _updateRegistry.UpdateCommandArgs;
        if (updateArgs is null)
            throw new InvalidOperationException("No options for ExternalUpdater set in registry.");

        var cpi = CurrentProcessInfo.Current;
        if (string.IsNullOrEmpty(cpi.ProcessFilePath))
            throw new NotSupportedException("The current process is not running from a file.");

        var loggingPath = string.IsNullOrEmpty(_logFileDirectory)
            ? _fileSystem.Path.GetTempPath()
            : _logFileDirectory;

        // Must be trimmed as otherwise paths enclosed in quotes and a trailing separator cause commandline arg parsing errors
        loggingPath = PathNormalizer.Normalize(loggingPath!, PathNormalizeOptions.TrimTrailingSeparators);

        var passThroughArgs = _applicationEnvironment
            .UpdateConfiguration.RestartConfiguration.PassCurrentArgumentsForRestart
            ? ExternalUpdaterArgumentUtilities.GetCurrentApplicationCommandLineForPassThrough()
            : null;

        var launchOptions = ExternalUpdaterArgumentUtilities.FromArgs(updateArgs)
            .WithCurrentData(
                cpi.ProcessFilePath!,
                passThroughArgs,
                cpi.Id,
                loggingPath,
                _serviceProvider);

        using var _ = new ExternalUpdaterLauncher(_serviceProvider).Start(updater, launchOptions);
    }
}

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
                Environment.Exit(RestartConstants.RestartRequiredCode);
        }
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

public static class ApplicationBaseServiceExtensions
{
    public static IServiceCollection MakeAppUpdateable(
        this IServiceCollection serviceCollection,
        UpdatableApplicationEnvironment applicationEnvironment,
        Func<IServiceProvider, IManifestLoader> manifestLoaderFactory)
    {
        if (applicationEnvironment == null) 
            throw new ArgumentNullException(nameof(applicationEnvironment));

        serviceCollection.TryAddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.TryAddSingleton<IResourceExtractor>(sp =>
            new CosturaResourceExtractor(applicationEnvironment.AssemblyInfo.Assembly, sp));

        serviceCollection.AddUpdateFramework();

        serviceCollection.AddSingleton<IExternalUpdaterLauncher>(sp => new ExternalUpdaterLauncher(sp));
        serviceCollection.AddSingleton<IProductService>(sp => new ApplicationProductService(sp));
        serviceCollection.AddSingleton<IBranchManager>(sp => new ApplicationBranchManager(applicationEnvironment, sp));
        serviceCollection.AddSingleton(manifestLoaderFactory);

        return serviceCollection;
    }
}