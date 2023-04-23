using AnakinRaW.AppUpdaterFramework.Conditions;
using AnakinRaW.AppUpdaterFramework.FileLocking;
using AnakinRaW.AppUpdaterFramework.Installer;
using AnakinRaW.AppUpdaterFramework.Interaction;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Storage;
using AnakinRaW.AppUpdaterFramework.Updater;
using AnakinRaW.AppUpdaterFramework.Updater.Handlers;
using AnakinRaW.AppUpdaterFramework.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework;

public static class LibraryInitialization
{
    public static void AddUpdateFramework(this IServiceCollection serviceCollection)
    {
        // All internal
        serviceCollection.AddSingleton<IVariableResolver>(_ => new VariableResolver());
        serviceCollection.AddSingleton<IUpdateCatalogProvider>(sp => new UpdateCatalogProvider(sp));
        serviceCollection.AddSingleton<IInstallerFactory>(sp => new InstallerFactory(sp));
        serviceCollection.AddSingleton<IDiskSpaceCalculator>(sp => new DiskSpaceCalculator(sp));
        serviceCollection.AddSingleton<IBackupManager>(sp => new BackupManager(sp));
        serviceCollection.AddSingleton<ILockedFileHandler>(sp => new LockedFileHandler(sp));
        serviceCollection.AddSingleton<ILockingProcessManagerFactory>(_ => new LockingProcessManagerFactory());
        serviceCollection.AddSingleton<IRestartManager>(_ => new RestartManager());
        serviceCollection.AddSingleton<IDownloadRepository>(sp => new DownloadRepository(sp));
        serviceCollection.AddSingleton<IBackupRepository>(sp => new BackupRepository(sp));
        serviceCollection.AddSingleton<IWritablePendingComponentStore>(new PendingComponentStore());


        // Internal implementation
        serviceCollection.AddSingleton<IUpdateService>(sp => new UpdateService(sp));
        serviceCollection.AddSingleton<IManifestInstallationDetector>(sp => new ManifestInstallationDetector(sp));
        serviceCollection.AddSingleton<IUpdateInteractionHandler>(sp => new DefaultUpdateInteractionHandler(sp));
        serviceCollection.AddSingleton<IUpdateHandler>(sp => new UpdateHandler(sp));
        serviceCollection.AddSingleton<IMetadataExtractor>(sp => new MetadataExtractor(sp));

        serviceCollection.AddSingleton<IReadonlyBackupManager>(sp => sp.GetRequiredService<IBackupManager>());
        serviceCollection.AddSingleton<IReadonlyDownloadRepository>(sp => sp.GetRequiredService<IDownloadRepository>());
        serviceCollection.AddSingleton<IPendingComponentStore>(sp => sp.GetRequiredService<IWritablePendingComponentStore>());

        serviceCollection.AddSingleton<IConditionEvaluatorStore>(_ =>
        {
            var conditionEvaluator = new ConditionEvaluatorStore();
            conditionEvaluator.AddConditionEvaluator(new FileConditionEvaluator());
            return conditionEvaluator;
        });

    }
}