using AnakinRaW.CommonUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace AnakinRaW.ApplicationBase.Utilities;

public class UnhandledExceptionHandler : DisposableObject
{
    protected readonly ILogger? Logger;
    protected readonly IServiceProvider Services;

    public UnhandledExceptionHandler(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        Logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            e.ParseUnhandledExceptionObject(out var message);
            var exception = e.ExceptionObject as Exception;
            if (e.IsTerminating)
                Logger?.LogCritical(exception, message);
            else
                Logger?.LogError(exception, message);
            HandleGlobalException(exception!, e.IsTerminating);
        }
        catch
        {
            // Ignore
        }
    }

    protected virtual void HandleGlobalException(Exception e, bool terminating)
    {
    }

    protected override void DisposeResources()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
    }
}