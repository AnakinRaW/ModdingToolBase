using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.ViewModels;
using AnakinRaW.CommonUtilities.Windows;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Dialog;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Commands.Handlers;

internal class ShowUpdateWindowCommandHandler(IServiceProvider serviceProvider)
    : AsyncCommandHandlerBase, IShowUpdateWindowCommandHandler
{
    private readonly IConnectionManager _connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();

    public override async Task HandleAsync()
    {
        // Singletone instance of this view model drastically increases closing/cancellation complexity.
        // Creating a new model for each request should be good enough. 
        var viewModel = new UpdateWindowViewModel(serviceProvider);
        await serviceProvider.GetRequiredService<IModalWindowService>().ShowModal(viewModel);
    }

    protected override bool CanHandle()
    {
        return _connectionManager.HasInternetConnection();
    }
}