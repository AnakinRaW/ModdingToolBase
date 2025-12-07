using System;
using System.IO.Abstractions;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.FileSystem.Normalization;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.ExternalUpdater;
using AnakinRaW.ExternalUpdater.Options;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AnakinRaW.ApplicationBase.Update;

internal sealed class SelfUpdateRestartHandler : IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly string? _logFileDirectory;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly UpdatableApplicationEnvironment _applicationEnvironment;
    private readonly ApplicationUpdateRegistry _updateRegistry;

    public SelfUpdateRestartHandler(
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

        if (ExternalUpdaterResultOptions.TryParse(args, out var externalUpdaterResult))
        {
            HandleRestartResult(externalUpdaterResult.Result, out var resetApplication);
            if (resetApplication)
                return SelfUpdateResult.Reset;
        }

        if (_updateRegistry.RequiresUpdate)
        {
            _logger?.LogDebug("Registry indicating update is required: Running external updater...");
            try
            {
                LaunchExternalUpdater();
                return SelfUpdateResult.RestartRequired;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Failed to run ExternalUpdater. Starting application normally: {EMessage}", e.Message);
                _updateRegistry.Reset();
            }
        }

        return SelfUpdateResult.Success;
    }

    public void Dispose()
    {
        _updateRegistry.Dispose();
    }

    private void HandleRestartResult(ExternalUpdaterResult result, out bool shouldReset)
    {
        shouldReset = false;
        _logger?.LogTrace("ExternalUpdater result: '{Result}'", result);

        if (result == ExternalUpdaterResult.UpdateFailedNoRestore || _updateRegistry.ResetApp)
        {
            _logger?.LogDebug("Resetting app due to ExternalUpdater result '{Result}' or UpdateRegistry/ResetApp = {Reset}", result, _updateRegistry.ResetApp);
            _updateRegistry.Reset();
            shouldReset = true;
            return;
        }

        if (result is ExternalUpdaterResult.UpdateFailedWithRestore or ExternalUpdaterResult.UpdateSuccess)
        {
            _logger?.LogDebug("ExternalUpdater indicated result '{Result}'.", result);
            _updateRegistry.Reset();
        }
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
            ? ExternalUpdaterArgumentUtilities.GetCurrentApplicationCommandLineForPassThroughAsBase64()
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