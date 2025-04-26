using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Services;

namespace AnakinRaW.ApplicationBase.New;

public enum SelfUpdateResult
{
    None,
    Success,
    Reset,
    RestartRequired
}

public sealed class SelfUpdatableAppBootstrapper(
    UpdatableApplicationEnvironment appEnvironment,
    IFileSystem fileSystem,
    IRegistry registry,
    Logger? logger) : IDisposable
{ 
    private readonly ApplicationUpdateRegistry _updateRegistry = new(registry, appEnvironment);

    public SelfUpdateResult HandleSelfUpdate(string[] args)
    {
        if (!appEnvironment.UpdateConfiguration.RestartConfiguration.SupportsRestart)
            return SelfUpdateResult.None;

        using var updateRegistry = new ApplicationUpdateRegistry(registry, appEnvironment);

        if (ExternalUpdaterResultOptions.TryParse(args, out var externalUpdaterResult) && !HandleRestartResult(externalUpdaterResult.Result))
            return SelfUpdateResult.Reset;

        logger?.Verbose("Application was not started with ExternalUpdater options.");

        if (_updateRegistry.RequiresUpdate)
        {
            logger?.Information("Registry indicating update is required: Running external updater...");
            try
            {
                LaunchExternalUpdater();
                logger?.Information("ExternalUpdater running. Closing application!");
                return SelfUpdateResult.RestartRequired;
            }
            catch (Exception e)
            {
                logger?.Error(e, $"Failed to run update. Starting main application normally: {e.Message}");
                updateRegistry.Reset();
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
        if (result == ExternalUpdaterResult.UpdateFailedNoRestore || _updateRegistry.ResetApp)
        {
            logger?.Debug($"Resetting app due to ExternalUpdater result '{result}' or UpdateRegistry/ResetApp = {_updateRegistry.ResetApp}");
            _updateRegistry.Reset();
            return false;
        }

        if (result is ExternalUpdaterResult.UpdateFailedWithRestore or ExternalUpdaterResult.UpdateSuccess)
        {
            logger?.Debug($"ExternalUpdater indicated result '{result}'.");
            _updateRegistry.Reset();
        }

        return true;
    }

    private void LaunchExternalUpdater()
    {
        var updaterPath = _updateRegistry.UpdaterPath;
        if (string.IsNullOrEmpty(updaterPath))
            throw new NotSupportedException("No updater in registry set");

        var updater = fileSystem.FileInfo.New(updaterPath!);

        var updateArgs = _updateRegistry.UpdateCommandArgs;
        if (updateArgs is null)
            throw new NotSupportedException("No updater options set.");

        var cpi = CurrentProcessInfo.Current;
        if (string.IsNullOrEmpty(cpi.ProcessFilePath))
            throw new InvalidOperationException("The current process is not running from a file.");

        // Must be trimmed as otherwise paths enclosed in quotes and a trailing separator
        // cause commandline arg parsing errors
        var loggingPath = PathNormalizer.Normalize(fileSystem.Path.GetTempPath(), PathNormalizeOptions.TrimTrailingSeparators);

        var launchOptions = ExternalUpdaterArgumentUtilities.FromArgs(updateArgs)
            .WithCurrentData(cpi.ProcessFilePath!, cpi.Id, loggingPath, serviceProvider);

        using var _ = new ExternalUpdaterLauncher().Start(updater, launchOptions);
    }
}

public abstract class SelfUpdateableAppLifecycle
{
    protected ApplicationEnvironment ApplicationEnvironment
    {
        get => LazyInitializer.EnsureInitialized(ref field, CreateAppEnvironment);
    } = null!;

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
        var coreServices = StartInternal(args);
        return RunAppAsync(args, coreServices).GetAwaiter().GetResult();
    }

    public async Task<int> StartAsync(string[] args)
    {
        var coreServices = StartInternal(args);
        // ConfigureAwait cannot be set to false here, because WPF apps might expect the context of the main thread.
        return await RunAppAsync(args, coreServices);
    }

    protected abstract ApplicationEnvironment CreateAppEnvironment();

    protected abstract IFileSystem CreateFileSystem();

    protected abstract IRegistry CreateRegistry();

    protected abstract Task<int> RunAppAsync(string[] args, IServiceCollection coreServices);

    protected abstract void ResetApp();

    private IServiceCollection StartInternal(string[] args)
    {
        using var logger = CreateBootstrapLogger();

        var serviceCollection = new ServiceCollection();

        serviceCollection.AddSingleton(FileSystem);
        serviceCollection.AddSingleton(Registry);
        serviceCollection.AddSingleton(ApplicationEnvironment);

        if (ApplicationEnvironment is UpdatableApplicationEnvironment updatableApplication)
        {
            serviceCollection.AddSingleton<IUpdateConfigurationProvider>(updatableApplication);
            using var updateBootstrapper = new SelfUpdatableAppBootstrapper(updatableApplication, FileSystem, Registry, logger);
            var selfUpdateResult = updateBootstrapper.HandleSelfUpdate(args);

            if (selfUpdateResult == SelfUpdateResult.Reset)
                ResetApp();
            if (selfUpdateResult == SelfUpdateResult.RestartRequired)
                Environment.Exit(RestartConstants.RestartRequiredCode);
        }

        return serviceCollection;
    }

    private Logger CreateBootstrapLogger()
    {
        var minLogLevel =
#if DEBUG
            LogEventLevel.Verbose;
#else
            LogEventLevel.Debug;
#endif

        var tempDir = FileSystem.Path.GetTempPath();
        var tempSubFolderName = FileSystem.Path.GetRandomFileName();
        var filePath = FileSystem.Path.GetFullPath(FileSystem.Path.Combine(tempDir, tempSubFolderName, "appBootstrap.log"));

        return new LoggerConfiguration()
            .WriteTo.File(filePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: minLogLevel)
            .CreateLogger();
    }
}

internal static class SelfUpdatableAppExtensions
{
    public static IServiceCollection MakeSelfUpdatable(this IServiceCollection services)
    {
        return services;
    }
}