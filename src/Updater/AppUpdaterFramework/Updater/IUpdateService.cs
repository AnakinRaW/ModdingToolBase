using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Updater;

public interface IUpdateService
{
    event EventHandler CheckingForUpdatesStarted;

    event EventHandler<IUpdateCatalog?> CheckingForUpdatesCompleted;

    event EventHandler<UpdateResult?> UpdateCompleted;

    event EventHandler<IUpdateSession> UpdateStarted;

    bool IsUpdating { get; }

    bool IsCheckingForUpdates { get; }

    Task<IUpdateCatalog?> CheckForUpdatesAsync(IProductReference productReference, CancellationToken token = default);
    
    Task<UpdateResult?> UpdateAsync(IUpdateCatalog updateCatalog, CancellationToken token = default);
}