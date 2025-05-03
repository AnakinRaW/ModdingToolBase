using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.AppUpdaterFramework.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public class UpdateHandler : IUpdateHandler
{
    private readonly IUpdateResultHandler _resultHandler;
    private readonly ILogger? _logger;
    private readonly IUpdateService _updateService;

    public bool IsUpdating { get; private set; }

    public bool IsCheckingForUpdate { get; private set; }

    public UpdateHandler(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _resultHandler = serviceProvider.GetRequiredService<IUpdateResultHandler>();
        _updateService = serviceProvider.GetRequiredService<IUpdateService>();

        _updateService.UpdateStarted += OnUpdateStarted;
        _updateService.UpdateCompleted += OnUpdateCompleted;
        _updateService.CheckingForUpdatesStarted += OnCheckingForUpdate;
        _updateService.CheckingForUpdatesCompleted += OnCheckingComplete;
    }

    public async Task<UpdateCheckResult> CheckForUpdateAsync(IProductReference productReference, CancellationToken token = default)
    {
        var updateCatalog = await _updateService.CheckForUpdatesAsync(productReference, token).ConfigureAwait(false);
        return new UpdateCheckResult(updateCatalog);
    }


    public async Task UpdateAsync(IUpdateCatalog parameter)
    {
        UpdateResult updateResult;
        try
        {
            updateResult = await _updateService.UpdateAsync(parameter).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, $"Unhandled exception {e.GetType()} encountered: {e.Message}");
            updateResult = new UpdateResult
            {
                Exception = e,
                IsCanceled = e.IsOperationCanceledException()
            };
        }

        await _resultHandler.Handle(updateResult).ConfigureAwait(false);
    }

    private void OnCheckingComplete(object sender, IUpdateCatalog? e)
    {
        IsCheckingForUpdate = false;
    }

    protected virtual void OnCheckingForUpdate(object sender, EventArgs e)
    {
        IsCheckingForUpdate = true;
    }

    protected virtual void OnUpdateCompleted(object sender, EventArgs e)
    {
        IsUpdating = false;
    }

    protected virtual void OnUpdateStarted(object sender, IUpdateSession e)
    {
        IsUpdating = true;
    }
}