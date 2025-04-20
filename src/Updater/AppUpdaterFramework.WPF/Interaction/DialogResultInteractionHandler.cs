using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Interaction;

internal class DialogResultInteractionHandler : IUpdateResultInteractionHandler
{
    private readonly IQueuedDialogService _dialogService;
    private readonly IUpdateDialogViewModelFactory _dialogViewModelFactory;

    public DialogResultInteractionHandler(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _dialogService = serviceProvider.GetRequiredService<IQueuedDialogService>();
        _dialogViewModelFactory = serviceProvider.GetRequiredService<IUpdateDialogViewModelFactory>();
    }

    public async Task<bool> ShallRestart(RestartReason reason)
    {
        var viewModel = _dialogViewModelFactory.CreateRestartViewModel(RestartReason.Update);
        var result = await _dialogService.ShowDialog(viewModel);
        return result == UpdateDialogButtonIdentifiers.RestartButtonIdentifier;
    }

    public Task ShowError(string message)
    {
        var viewModel = _dialogViewModelFactory.CreateErrorViewModel(message);
        return _dialogService.ShowDialog(viewModel);
    }
}