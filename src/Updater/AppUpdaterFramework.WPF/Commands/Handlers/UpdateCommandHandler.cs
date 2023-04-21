using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Commands.Handlers;

internal class UpdateCommandHandler : AsyncCommandHandlerBase<IUpdateCatalog>, IUpdateCommandHandler
{
    private readonly IUpdateHandler _updateHandler;
    private readonly IUpdateService _updateService;

    private readonly IAppDispatcher _dispatcher;
    private readonly IUpdateResultHandler _resultHandler;

    private bool _isUpdateInProgress;

    public UpdateCommandHandler(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _updateHandler = serviceProvider.GetRequiredService<IUpdateHandler>();
       
        _dispatcher = serviceProvider.GetRequiredService<IAppDispatcher>();

        _updateService = serviceProvider.GetRequiredService<IUpdateService>();
        _updateService.UpdateStarted += OnUpdateStarted;
        _updateService.UpdateCompleted += OnUpdateCompleted;
    }

    private void OnUpdateCompleted(object sender, EventArgs e)
    {
        _isUpdateInProgress = false;
        _dispatcher.BeginInvoke(DispatcherPriority.Background, CommandManager.InvalidateRequerySuggested);
    }

    private void OnUpdateStarted(object sender, IUpdateSession e)
    {
        _isUpdateInProgress = true;
        _dispatcher.BeginInvoke(DispatcherPriority.Background, CommandManager.InvalidateRequerySuggested);
    }

    public override Task HandleAsync(IUpdateCatalog parameter)
    {
        return _updateHandler.HandleAsync(parameter);
    }

    public override bool CanHandle(IUpdateCatalog? parameter)
    {
        return !_isUpdateInProgress;
    }

    private Task<UpdateResult> UpdateAsync(IUpdateCatalog parameter)
    {
        return _updateService.Update(parameter);
    }
}