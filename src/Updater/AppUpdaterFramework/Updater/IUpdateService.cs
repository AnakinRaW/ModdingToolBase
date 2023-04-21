using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Updater;

public interface IUpdateService
{
    event EventHandler CheckingForUpdatesStarted;

    event EventHandler<IUpdateCatalog?> CheckingForUpdatesCompleted;

    event EventHandler<IUpdateSession> UpdateStarted;

    event EventHandler UpdateCompleted;

    Task CheckForUpdates(IProductReference productReference, CancellationToken token = default);

    Task<UpdateResult> Update(IUpdateCatalog updateCatalog);
}

public interface IUpdateHandler
{
    Task HandleAsync(IUpdateCatalog parameter);
}

internal sealed class UpdateHandler : IUpdateHandler
{
    private readonly IUpdateResultHandler _resultHandler;
    private readonly ILogger? _logger;
    private readonly IUpdateService _updateService;

    public UpdateHandler(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _resultHandler = serviceProvider.GetRequiredService<IUpdateResultHandler>();
        _updateService = serviceProvider.GetRequiredService<IUpdateService>();
    }

    public async Task HandleAsync(IUpdateCatalog parameter)
    {
        UpdateResult updateResult;
        try
        {
            updateResult = await _updateService.Update(parameter).ConfigureAwait(false);
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
}