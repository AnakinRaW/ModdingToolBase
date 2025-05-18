using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;

namespace AnakinRaW.AppUpdaterFramework.Updater;

public interface IUpdateService
{
    event EventHandler CheckingForUpdatesStarted;

    event EventHandler<UpdateCatalog?> CheckingForUpdatesCompleted;

    event EventHandler<UpdateResult?> UpdateCompleted;

    event EventHandler<IUpdateSession> UpdateStarted;

    bool IsUpdating { get; }

    bool IsCheckingForUpdates { get; }

    Task<UpdateCatalog?> CheckForUpdatesAsync(ProductReference productReference, CancellationToken token = default);
    
    Task<UpdateResult?> UpdateAsync(UpdateCatalog updateCatalog, CancellationToken token = default);
}