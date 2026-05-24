using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public class WpfUpdateResultHandler(IServiceProvider serviceProvider) : UpdateResultHandler(serviceProvider), IUpdateResultHandler
{
    private readonly IApplicationShutdownService _shutdownService = serviceProvider.GetRequiredService<IApplicationShutdownService>();
    private readonly IQueuedDialogService _dialogService = serviceProvider.GetRequiredService<IQueuedDialogService>();
    private readonly IUpdateDialogViewModelFactory _dialogViewModelFactory = serviceProvider.GetRequiredService<IUpdateDialogViewModelFactory>();

    protected override void Shutdown()
    {
        _shutdownService.Shutdown(RestartConstants.RestartRequiredCode);
    }

    protected override async Task<bool> ShallRestart(RestartReason reason)
    {
        var viewModel = _dialogViewModelFactory.CreateRestartViewModel(reason);
        var result = await _dialogService.ShowDialog(viewModel);
        return result == UpdateDialogButtonIdentifiers.RestartButtonIdentifier;
    }

    protected override Task ShowError(UpdateResult updateResult)
    {
        var message = updateResult.ErrorMessage;
        if (string.IsNullOrEmpty(message))
            return Task.CompletedTask;
        var viewModel = _dialogViewModelFactory.CreateErrorViewModel(message);
        return _dialogService.ShowDialog(viewModel);
    }
}
