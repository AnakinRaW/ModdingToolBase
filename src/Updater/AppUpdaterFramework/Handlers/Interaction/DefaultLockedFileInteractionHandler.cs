using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Handlers.Interaction;

internal class DefaultLockedFileInteractionHandler : ILockedFileInteractionHandler
{
    private readonly ILogger? _logger;

    public DefaultLockedFileInteractionHandler(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public LockedFileHandlerInteractionResult HandleLockedFile(string file, IEnumerable<ILockingProcess> lockingProcesses)
    {
        _logger?.LogTrace("Interaction Result: {Result}", LockedFileHandlerInteractionResult.Cancel);
        return LockedFileHandlerInteractionResult.Cancel;
    }
    
    public void HandleError(string message)
    {
        _logger?.LogTrace("Interaction Error: {Message}", message);
    }
}