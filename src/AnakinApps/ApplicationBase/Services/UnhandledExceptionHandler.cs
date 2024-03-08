using System;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.CommonUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Services;

internal class UnhandledExceptionHandler : DisposableObject, IUnhandledExceptionHandler
{
    private readonly ILogger? _logger;

    protected IServiceProvider Services { get; }

    public UnhandledExceptionHandler(IServiceProvider services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            e.ParseUnhandledExceptionObject(out var message);
            var exception = e.ExceptionObject as Exception;
            if (e.IsTerminating)
                _logger?.LogCritical(exception, message);
            else
                _logger?.LogError(exception, message);

            HandleGlobalException(exception!);
        }
        catch
        {
            // Ignore
        }
    }

    protected virtual void HandleGlobalException(Exception e)
    {
    }

    protected override void DisposeManagedResources()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
    }
}