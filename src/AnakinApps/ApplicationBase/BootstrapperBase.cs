﻿using System;
using System.IO.Abstractions;
using AnakinRaW.ApplicationBase.Update;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.ExternalUpdater;
using AnakinRaW.ExternalUpdater.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AnakinRaW.ApplicationBase.Services;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.ApplicationBase.Update.External;
using AnakinRaW.CommonUtilities.Registry;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.AppUpdaterFramework.Handlers;
using Testably.Abstractions;

namespace AnakinRaW.ApplicationBase;

public abstract class BootstrapperBase
{
    protected abstract IApplicationEnvironment CreateEnvironment(IServiceProvider serviceProvider);

    protected abstract IRegistry CreateRegistry();

    protected abstract int Execute(string[] args, IServiceCollection serviceCollection);

    private protected virtual void CreateCoreServices(IServiceCollection serviceCollection) { }

    protected int Run(string[] args)
    {
        var serviceCollection = CreateCoreServices();
        var coreServices = serviceCollection.BuildServiceProvider();

        if (ExternalUpdaterResultOptions.TryParse(args, out var externalUpdaterOptions))
        {
            var registry = coreServices.GetRequiredService<IApplicationUpdaterRegistry>();
            new ExternalUpdaterResultHandler(registry).Handle(externalUpdaterOptions!.Result);
        }

        // Since logging directory is not yet assured, we cannot run under the global exception handler.
        var resetHandler = coreServices.GetRequiredService<IAppResetHandler>();
        resetHandler.ResetIfNecessary();

        using (coreServices.GetRequiredService<IUnhandledExceptionHandler>())
        {
            return ExecuteInternal(args, serviceCollection);
        }
    }

    private protected virtual void CreateApplicationServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddUpdateFramework();

        serviceCollection.AddSingleton<IExternalUpdateExtractor>(sp => new ExternalUpdateExtractor(sp));

        serviceCollection.AddSingleton<IProductService>(sp => new ApplicationProductService(sp));

        serviceCollection.AddSingleton<IBranchManager>(sp => new ApplicationBranchManager(sp));
        serviceCollection.AddSingleton<IUpdateConfigurationProvider>(sp => new ApplicationUpdateConfigurationProvider(sp));
        serviceCollection.AddSingleton<IManifestLoader>(sp => new JsonManifestLoader(sp));
        serviceCollection.AddSingleton<IUpdateResultHandler>(sp => new AppUpdateResultHandler(sp));

        serviceCollection.AddSingleton<IHashingService>(sp => new HashingService(sp));
    }


    private int ExecuteInternal(string[] args, IServiceCollection serviceCollection)
    {
        var coreServices = serviceCollection.BuildServiceProvider();
        var logger = coreServices.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        var env = coreServices.GetRequiredService<IApplicationEnvironment>();
        var updateRegistry = coreServices.GetRequiredService<IApplicationUpdaterRegistry>();


        logger?.LogTrace($"Application Version: {env.AssemblyInfo.InformationalVersion}");
        logger?.LogTrace($"Raw Command line: {Environment.CommandLine}");
        
        if (updateRegistry.RequiresUpdate)
        {
            logger?.LogInformation("Registry indicating update is required: Running external updater...");
            try
            {
                coreServices.GetRequiredService<IRegistryExternalUpdaterLauncher>().Launch();
                logger?.LogInformation("External updater running. Closing application!");
                return RestartConstants.RestartRequiredCode;
            }
            catch (Exception e)
            {
                logger?.LogError(e, $"Failed to run update. Starting main application normally: {e.Message}");
                updateRegistry.Clear();
            }
        }

        CreateApplicationServices(serviceCollection);
        
        var exitCode = Execute(args, serviceCollection);
        logger?.LogTrace($"Exit Code: {exitCode}");

        return exitCode;
    }

    private IServiceCollection CreateCoreServices()
    {
        var serviceCollection = new ServiceCollection();
        
        var fileSystem = new RealFileSystem();
        serviceCollection.AddSingleton<IFileSystem>(fileSystem);
        serviceCollection.AddSingleton<ICurrentProcessInfoProvider>(_ => new CurrentProcessInfoProvider());

        serviceCollection.AddSingleton(CreateRegistry());
        serviceCollection.AddSingleton<IRegistryExternalUpdaterLauncher>(sp => new RegistryExternalUpdaterLauncher(sp));

        using var environmentServiceProvider = serviceCollection.BuildServiceProvider();
        var environment = CreateEnvironment(environmentServiceProvider);
        serviceCollection.AddSingleton(environment);

        serviceCollection.AddSingleton<IApplicationUpdaterRegistry>(sp => new ApplicationUpdaterRegistry(environment.ApplicationRegistryPath, sp));
        serviceCollection.AddSingleton<IExternalUpdaterLauncher>(sp => new ExternalUpdaterLauncher(sp));
        serviceCollection.AddSingleton<IResourceExtractor>(sp => new CosturaResourceExtractor(environment.AssemblyInfo.Assembly, sp));

        serviceCollection.AddSingleton<IAppResetHandler>(sp => new AppResetHandler(sp));
        serviceCollection.AddSingleton<IUnhandledExceptionHandler>(sp => new UnhandledExceptionHandler(sp));

        serviceCollection.AddLogging(builder => ConfigureLogging(builder, fileSystem, environment));
        CreateCoreServices(serviceCollection);

        return serviceCollection;
    }

    protected virtual void ConfigureLogging(ILoggingBuilder loggingBuilder, IFileSystem fileSystem, IApplicationEnvironment applicationEnvironment)
    {
    }
}