﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.FileLocking.Interaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Interaction;

internal class DefaultUpdateInteractionHandler : IUpdateInteractionHandler
{
    private readonly ILogger? _logger;

    public DefaultUpdateInteractionHandler(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public LockedFileHandlerInteractionResult HandleLockedFile(IFileInfo file, IEnumerable<ILockingProcess> lockingProcesses)
    {
        _logger?.LogTrace($"Interaction Result: {LockedFileHandlerInteractionResult.Cancel}");
        return LockedFileHandlerInteractionResult.Cancel;
    }

    public void HandleError(string message)
    {
        _logger?.LogTrace($"Interaction Error: {message}");
    }
}