using System;
using System.Threading;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
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

    /// <summary>
    /// Updates the application using the provided update manifest asynchronously.
    /// </summary>
    /// <param name="manifest">The update manifest to apply.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous update operation.
    /// The task result contains an <see cref="UpdateResult"/> indicating the outcome of the update process.
    /// </returns>
    Task<UpdateResult?> UpdateAsync(ProductManifest manifest, CancellationToken token = default);
}