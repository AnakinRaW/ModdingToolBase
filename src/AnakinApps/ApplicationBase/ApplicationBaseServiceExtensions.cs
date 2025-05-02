using System;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnakinRaW.ApplicationBase;

public static class ApplicationBaseServiceExtensions
{
    public static IServiceCollection MakeAppUpdateable(
        this IServiceCollection serviceCollection,
        UpdatableApplicationEnvironment applicationEnvironment,
        Func<IServiceProvider, IProductService> productServiceFactory,
        Func<IServiceProvider, IManifestLoader> manifestLoaderFactory)
    {
        if (applicationEnvironment == null) 
            throw new ArgumentNullException(nameof(applicationEnvironment));

        serviceCollection.TryAddSingleton<IHashingService>(sp => new HashingService(sp));

        serviceCollection.AddUpdateFramework();

        serviceCollection.AddSingleton(productServiceFactory);
        serviceCollection.AddSingleton(manifestLoaderFactory);
        serviceCollection.AddSingleton<IBranchManager>(sp => new ApplicationBranchManager(applicationEnvironment, sp));

        if (applicationEnvironment.UpdateConfiguration.RestartConfiguration.SupportsRestart) 
            serviceCollection.AddSingleton<IExternalUpdaterLauncher>(sp => new ExternalUpdaterLauncher(sp));

        return serviceCollection;
    }
}