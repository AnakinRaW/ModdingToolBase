using System;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public class WpfUpdateHandler : IUpdateHandler
{
    private readonly IAppDispatcher _dispatcher;
    private readonly IUpdateService _updateService;
    private readonly IUpdateResultHandler _resultHandler;
    private readonly ILogger? _logger;

    public bool IsUpdating { get; private set; }

    public WpfUpdateHandler(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _dispatcher = serviceProvider.GetRequiredService<IAppDispatcher>();
        _updateService = serviceProvider.GetRequiredService<IUpdateService>();
        _resultHandler = serviceProvider.GetRequiredService<IUpdateResultHandler>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        _updateService.UpdateStarted += OnUpdateStarted!;
        _updateService.UpdateCompleted += OnUpdateCompleted!;
    }

    public async Task UpdateAsync(UpdateCatalog updateCatalog)
    {
        UpdateResult? updateResult;
        try
        {
            updateResult = await _updateService.UpdateAsync(updateCatalog).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Unhandled exception {Type} encountered: {Message}", e.GetType(), e.Message);
            updateResult = new UpdateResult
            {
                Exception = e,
                IsCanceled = e is OperationCanceledException
            };
        }

        if (updateResult is null)
            return;
        await _resultHandler.Handle(updateResult).ConfigureAwait(false);
    }

    protected virtual void OnUpdateCompleted(object sender, UpdateResult? e)
    {
        IsUpdating = false;
        _dispatcher.BeginInvoke(DispatcherPriority.Background, CommandManager.InvalidateRequerySuggested);
    }

    protected virtual void OnUpdateStarted(object sender, IUpdateSession e)
    {
        IsUpdating = true;
        _dispatcher.BeginInvoke(DispatcherPriority.Background, CommandManager.InvalidateRequerySuggested);
    }
}
