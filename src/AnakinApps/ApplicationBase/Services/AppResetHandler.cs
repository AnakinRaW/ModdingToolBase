using System;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Services;

internal class AppResetHandler
{
    private readonly ApplicationUpdateRegistry _registry;
    private readonly ApplicationEnvironment _environment;
    private readonly ILogger? _logger;

    protected IServiceProvider Services { get; }

    public AppResetHandler(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        _registry = services.GetRequiredService<ApplicationUpdateRegistry>();
        _environment = services.GetRequiredService<ApplicationEnvironment>();
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
            _environment.ApplicationLocalDirectory.DeleteWithRetry();
            _registry.Reset();
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
        return !_environment.ApplicationLocalDirectory.Exists;
    }

    protected virtual void OnReset()
    {
    }

    protected virtual void OnResetFailed(Exception exception)
    {
    }
}