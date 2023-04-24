using System;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Updater;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public class UpdateResultHandler : IUpdateResultHandler
{
    private readonly IUpdateConfiguration _updateConfiguration;
    private readonly IRestartHandler _restartHandler;
    private readonly IUpdateResultInteractionHandler _interactionHandler;

    public UpdateResultHandler(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _interactionHandler = serviceProvider.GetRequiredService<IUpdateResultInteractionHandler>();
        _restartHandler = serviceProvider.GetRequiredService<IRestartHandler>();
        _updateConfiguration = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
    }

    public async Task Handle(UpdateResult result)
    {
        if (result.RestartType == RestartType.ApplicationElevation)
        {
            await HandleRestartInternal(RestartReason.Elevation);
            return;
        }

        if (result.FailedRestore)
        {
            await HandleRestartInternal(RestartReason.FailedRestore);
            return;
        }

        if (result.RestartType == RestartType.ApplicationRestart)
        {
            await HandleRestartInternal(RestartReason.Update);
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
        var message = updateResult.Exception is AggregateException aggregateException
            ? aggregateException.InnerExceptions.First().Message
            : updateResult.Exception!.Message;

        return _interactionHandler.ShowError(message);
    }

    protected virtual async Task HandleRestart(RestartReason reason)
    {
        var shallRestart = await _interactionHandler.ShallRestart(reason);
        if (!shallRestart)
            return;

        var restartKind = reason switch
        {
            RestartReason.Update => RequiredRestartOptionsKind.Update,
            RestartReason.Elevation => RequiredRestartOptionsKind.RestartElevated,
            RestartReason.FailedRestore => RequiredRestartOptionsKind.Restart,
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };

        _restartHandler.Restart(restartKind);
    }

    private Task HandleRestartInternal(RestartReason reason)
    {
        return !_updateConfiguration.SupportsRestart ? Task.CompletedTask : HandleRestart(reason);
    }
}