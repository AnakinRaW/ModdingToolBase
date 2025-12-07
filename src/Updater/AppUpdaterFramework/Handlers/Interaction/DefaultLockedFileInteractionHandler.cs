using System;
using System.Collections.Generic;
using System.IO.Abstractions;
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

    public LockedFileHandlerInteractionResult HandleLockedFile(IFileInfo file, IEnumerable<ILockingProcess> lockingProcesses)
    {
        _logger?.LogTrace("Interaction Result: {Result}", LockedFileHandlerInteractionResult.Cancel);
        return LockedFileHandlerInteractionResult.Cancel;
    }
    
    public void HandleError(string message)
    {
        _logger?.LogTrace("Interaction Error: {Message}", message);
    }
}