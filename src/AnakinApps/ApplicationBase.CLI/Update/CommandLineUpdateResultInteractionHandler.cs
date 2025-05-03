using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Update;

internal class CommandLineUpdateResultInteractionHandler : IUpdateResultInteractionHandler
{
    private readonly bool _automaticRestart;
    private readonly ILogger? _logger;

    public CommandLineUpdateResultInteractionHandler(bool automaticRestart, IServiceProvider serviceProvider)
    {
        _automaticRestart = automaticRestart;
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public Task<bool> ShallRestart(RestartReason reason)
    {
        var reasonText = reason switch
        {
            RestartReason.Update => "An update is required.",
            RestartReason.Elevation => "The application needs to run with admin rights.",
            RestartReason.FailedRestore => "An internal error occurred and the application needs to be restored.",
            _ => "Unknown reason."
        };

        var message = $"Application needs to be restarted. Reason: {reasonText}";
        _logger?.LogInformation(message);

        if (_automaticRestart)
            return Task.FromResult(true);

        Console.WriteLine(message);
        return Task.FromResult(ConsoleUtilities.UserYesNoQuestion("Do you want to restart now?"));
    }

    public Task ShowError(string message)
    {
        _logger?.LogWarning($"Error during update: {message}");
        return Task.CompletedTask;
    }
}