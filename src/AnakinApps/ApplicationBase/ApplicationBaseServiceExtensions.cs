using System;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Handlers;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AnakinRaW.ApplicationBase;

public static class ApplicationBaseServiceExtensions
{ 
    public static IServiceCollection MakeAppUpdateable(
        this IServiceCollection serviceCollection,
        UpdatableApplicationEnvironment applicationEnvironment,
        Func<IServiceProvider, IProductService> productServiceFactory,
        Func<IServiceProvider, IManifestLoader> manifestLoaderFactory,
        Action<IServiceCollection>? additionalUpdateServices = null)
    {
        if (applicationEnvironment == null)
            throw new ArgumentNullException(nameof(applicationEnvironment));

        serviceCollection.AddSingleton(productServiceFactory);
        serviceCollection.AddSingleton(manifestLoaderFactory);
        serviceCollection.AddSingleton<IBranchManager>(sp => new ApplicationBranchManager(applicationEnvironment, sp));

        additionalUpdateServices?.Invoke(serviceCollection);

        serviceCollection.TryAddSingleton<IHashingService>(sp => new HashingService(sp));
        serviceCollection.TryAddSingleton<IUpdateResultHandler>(sp => new ApplicationUpdateResultHandler(applicationEnvironment, sp));
        
        serviceCollection.AddUpdateFramework();

        return serviceCollection;
    }
}