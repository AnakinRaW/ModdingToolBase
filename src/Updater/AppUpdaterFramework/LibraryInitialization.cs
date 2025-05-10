using AnakinRaW.AppUpdaterFramework.Detection;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Handlers.Interaction;
using AnakinRaW.AppUpdaterFramework.Installer;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnakinRaW.AppUpdaterFramework;

public static class LibraryInitialization
{
    public static void AddUpdateFramework(this IServiceCollection serviceCollection)
    {
        // All internal
        serviceCollection.AddSingleton<IUpdateCatalogProvider>(sp => new UpdateCatalogProvider(sp));
        serviceCollection.AddSingleton<IInstallerFactory>(sp => new InstallerFactory(sp));
        serviceCollection.AddSingleton<IDiskSpaceCalculator>(sp => new DiskSpaceCalculator(sp));
        serviceCollection.AddSingleton<IBackupManager>(sp => new BackupManager(sp));
        serviceCollection.AddSingleton<IReadOnlyBackupManager>(sp => sp.GetRequiredService<IBackupManager>());
        serviceCollection.AddSingleton<IDownloadRepositoryFactory>(sp => new DownloadRepositoryFactory(sp));
        serviceCollection.AddSingleton<ILockedFileHandler>(sp => new LockedFileHandler(sp));
        serviceCollection.AddSingleton<IRestartManager>(_ => new RestartManager());
        serviceCollection.AddSingleton<IWritablePendingComponentStore>(new PendingComponentStore());

        // Default implementations
        serviceCollection.TryAddSingleton<IUpdateService>(sp => new UpdateService(sp));
        serviceCollection.TryAddSingleton<IComponentInstallationDetector>(sp => new ComponentInstallationDetector(sp));
        serviceCollection.TryAddSingleton<IManifestInstallationDetector>(sp => new ManifestInstallationDetector(sp));
        serviceCollection.TryAddSingleton<IMetadataExtractor>(sp => new MetadataExtractor(sp));
        serviceCollection.TryAddSingleton<IPendingComponentStore>(sp => sp.GetRequiredService<IWritablePendingComponentStore>());
        serviceCollection.TryAddSingleton<IExternalUpdaterService>(sp => new ExternalUpdaterService(sp));
        serviceCollection.TryAddSingleton<IExternalUpdaterLauncher>(sp => new ExternalUpdaterLauncher(sp));

        serviceCollection.TryAddSingleton<ILockedFileInteractionHandler>(sp => new DefaultLockedFileInteractionHandler(sp));
    }
}