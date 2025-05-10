using System;
using System.Threading.Tasks;
using AnakinRaW.CommonUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Utilities;

public class UnobservedTaskExceptionHandler : DisposableObject
{
    private readonly ILogger? _logger;

    public UnobservedTaskExceptionHandler(IServiceProvider services)
    {
        _logger = services.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        TaskScheduler.UnobservedTaskException += OnUnobservedException;
    }

    private void OnUnobservedException(object sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, e.ToString());
        OnUnobservedException(e.Exception, e.Observed);
    }

    protected virtual void OnUnobservedException(Exception e, bool observed)
    {
    }
    
    protected override void DisposeResources()
    {
        TaskScheduler.UnobservedTaskException -= OnUnobservedException;
    }
}