using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Interaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase;

internal class CommandLineResultInteractionHandler : IUpdateResultInteractionHandler
{
    private readonly ILogger? _logger;

    public CommandLineResultInteractionHandler(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public Task<bool> ShallRestart(RestartReason reason)
    {
        return Task.FromResult(true);
    }

    public Task ShowError(string message)
    {
        _logger?.LogTrace(message);
        Console.WriteLine($"Error during update: {message}");
        return Task.CompletedTask;
    }
}