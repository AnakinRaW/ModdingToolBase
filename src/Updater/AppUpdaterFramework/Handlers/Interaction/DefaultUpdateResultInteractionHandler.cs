using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Handlers.Interaction;

internal class DefaultUpdateResultInteractionHandler(IServiceProvider serviceProvider) : IUpdateResultInteractionHandler
{
    private readonly ILogger? _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(DefaultUpdateResultInteractionHandler));
    private readonly UpdateConfiguration _updateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();

    public Task<bool> ShallRestart(RestartReason reason)
    {
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

        return Task.FromResult(_updateConfiguration.RestartConfiguration.SupportsRestart);
    }

    public Task ShowError(string message)
    {
        _logger?.LogWarning($"Error during update: {message}");
        return Task.CompletedTask;
    }
}