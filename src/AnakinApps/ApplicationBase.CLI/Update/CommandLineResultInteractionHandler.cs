using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
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

        var reasonText = "Unknown Reason";
        switch (reason)
        {
            case RestartReason.Update:
                reasonText = "An update is required.";
                break;
            case RestartReason.Elevation:
                reasonText = "The application needs to run with admin rights.";
                break;
            case RestartReason.FailedRestore:
                reasonText = "An internal error occurred and the application needs to be restored.";
                break;
        }

        var message = $"Application needs to be restarted. Reason: {reasonText}";

        _logger?.LogWarning(message);
        return Task.FromResult(options.AutomaticRestart);
    }

    public Task ShowError(string message)
    {
        _logger?.LogWarning($"Error during update: {message}");
        return Task.CompletedTask;
    }
}