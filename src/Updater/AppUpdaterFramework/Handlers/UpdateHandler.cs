using System;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.AppUpdaterFramework.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Handlers;

public class UpdateHandler : IUpdateHandler
{
    private readonly IUpdateResultHandler _resultHandler;
    private readonly ILogger? _logger;
    private readonly IUpdateService _updateService;

    public bool IsUpdating { get; private set; }

    public UpdateHandler(IServiceProvider serviceProvider)
    {
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _resultHandler = serviceProvider.GetRequiredService<IUpdateResultHandler>();
        _updateService = serviceProvider.GetRequiredService<IUpdateService>();

        _updateService.UpdateStarted += OnUpdateStarted;
        _updateService.UpdateCompleted += OnUpdateCompleted;
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

    protected virtual void OnUpdateCompleted(object sender, EventArgs e)
    {
        IsUpdating = false;
    }

    protected virtual void OnUpdateStarted(object sender, IUpdateSession e)
    {
        IsUpdating = true;
    }
}