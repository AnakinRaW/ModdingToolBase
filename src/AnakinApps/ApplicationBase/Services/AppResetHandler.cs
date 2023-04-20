using System;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.ApplicationBase.Services;

internal class AppResetHandler : IAppResetHandler
{
    private readonly IApplicationUpdaterRegistry _registry;
    private readonly IFileSystemService _fileSystemService;
    private readonly IApplicationEnvironment _environment;
    private readonly ILogger? _logger;

    protected IServiceProvider Services { get; }

    public AppResetHandler(IServiceProvider services)
    {
        Requires.NotNull(services, nameof(services));
        Services = services;
        _registry = services.GetRequiredService<IApplicationUpdaterRegistry>();
        _fileSystemService = services.GetRequiredService<IFileSystemService>();
        _environment = services.GetRequiredService<IApplicationEnvironment>();
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public void ResetIfNecessary()
    {
        if (RequiresReset())
            ResetApplication();
    }

    public void ResetApplication()
    {
        try
        {
            _fileSystemService.DeleteDirectoryWithRetry(_environment.ApplicationLocalDirectory);
            _registry.Clear();
            _environment.ApplicationLocalDirectory.Create();

            OnReset();
        }
        catch (Exception e)
        {
            _logger?.LogCritical(e, e.Message);
            OnResetFailed(e);
            throw;
        }
    }

    protected virtual bool RequiresReset()
    {
        return !_environment.ApplicationLocalDirectory.Exists || _registry.Reset;
    }

    protected virtual void OnReset()
    {
    }

    protected virtual void OnResetFailed(Exception exception)
    {
    }
}