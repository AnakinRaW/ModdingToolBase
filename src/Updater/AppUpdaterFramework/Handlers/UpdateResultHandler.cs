using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.ExternalUpdater.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.External;
using Microsoft.Extensions.Logging;
using IServiceProvider = System.IServiceProvider;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public class UpdateResultHandler
{
    protected readonly IServiceProvider ServiceProvider;
    protected readonly UpdateConfiguration UpdateConfiguration;
    protected readonly IExternalUpdaterService ExternalUpdaterService;
    protected readonly ILogger? Logger;

    public UpdateResultHandler(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        UpdateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
        ExternalUpdaterService = serviceProvider.GetRequiredService<IExternalUpdaterService>();
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
    }

    public async Task Handle(UpdateResult result)
    {
        if (result.RestartType == RestartType.ApplicationElevation)
        {
            await HandleRestart(RestartReason.Elevation);
            return;
        }

        if (result.FailedRestore)
        {
            await HandleRestart(RestartReason.RestoreFailed);
            return;
        }

        if (result.RestartType == RestartType.ApplicationRestart)
        {
            await HandleRestart(RestartReason.Update);
            return;
        }

        if (result.Exception is null || result.IsCanceled)
        {
            await HandleSuccess();
            return;
        }

        await ShowError(result);
    } 
    
    protected virtual Task HandleSuccess()
    {
        return Task.CompletedTask;
    }

    protected virtual Task ShowError(UpdateResult updateResult)
    {
        Logger?.LogError("The update failed with an error: " + updateResult.ErrorMessage);
        return Task.CompletedTask;
    }
    protected virtual Task<bool> ShallRestart(RestartReason reason)
    {
        return Task.FromResult(UpdateConfiguration.RestartConfiguration.SupportsRestart);
    }

    protected virtual void RestartApplication(RestartReason reason)
    {
        var restartOptions = CreateOptions(reason);
        ExternalUpdaterService.Launch(restartOptions);
        Shutdown();
    }
    
    protected virtual void Shutdown()
    {
        Environment.Exit(RestartConstants.RestartRequiredCode);
    }

    private async Task HandleRestart(RestartReason reason)
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
            case RestartReason.RestoreFailed:
                reasonText = "An internal error occurred and the application needs to be restored.";
                break;
        }

        var message = $"Application needs to be restarted. Reason: {reasonText}";
        Logger?.LogWarning(message);

        if (!UpdateConfiguration.RestartConfiguration.SupportsRestart || !await ShallRestart(reason))
            return;
        RestartApplication(reason);
    }

    private ExternalUpdaterOptions CreateOptions(RestartReason restartReason)
    {
        return restartReason switch
        {
            RestartReason.RestoreFailed => ExternalUpdaterService.CreateRestartOptions(),
            RestartReason.Elevation => ExternalUpdaterService.CreateRestartOptions(true),
            RestartReason.Update => ExternalUpdaterService.CreateUpdateOptions(),
            _ => throw new ArgumentOutOfRangeException(nameof(restartReason), restartReason, null)
        };
    }
}