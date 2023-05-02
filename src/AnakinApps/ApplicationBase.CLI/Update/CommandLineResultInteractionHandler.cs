using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Interaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Update;

internal class CommandLineResultInteractionHandler : IUpdateResultInteractionHandler
{
    private readonly ILogger? _logger;
    private readonly IUpdateOptionsProvider _optionsProvider;

    public CommandLineResultInteractionHandler(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _optionsProvider = serviceProvider.GetRequiredService<IUpdateOptionsProvider>();
    }

    public Task<bool> ShallRestart(RestartReason reason)
    {
        var options = _optionsProvider.GetOptions();
        return Task.FromResult(options.AutomaticRestart);
    }

    public Task ShowError(string message)
    {
        _logger?.LogWarning($"Error during update: {message}");
        return Task.CompletedTask;
    }
}