using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Interaction;

internal abstract class InteractiveHandlerBase
{
    protected readonly ILogger? Logger;
    protected IUpdateInteractionHandler UpdateInteractionHandler { get; }

    protected InteractiveHandlerBase(IServiceProvider serviceProvider)
    {
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        UpdateInteractionHandler = serviceProvider.GetRequiredService<IUpdateInteractionHandler>();
    }

    protected void HandleError(string message)
    {
        UpdateInteractionHandler.HandleError(message);
    }
}