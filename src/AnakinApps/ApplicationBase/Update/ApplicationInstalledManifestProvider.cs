using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

public class ApplicationInstalledManifestProvider : IInstalledManifestProvider
{
    private readonly IApplicationEnvironment _applicationEnvironment;

    protected readonly IResourceExtractor ResourceExtractor;
    protected readonly IMetadataExtractor MetadataExtractor;

    public ApplicationInstalledManifestProvider(IServiceProvider serviceProvider)
    {
        _applicationEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();

        ResourceExtractor = serviceProvider.GetRequiredService<IResourceExtractor>();
        MetadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();
    }

    public IProductManifest ProvideManifest(IProductReference installedProduct, ProductVariables variables)
    {
        var application = _applicationEnvironment.AssemblyInfo.Assembly;
        var appComponent = MetadataExtractor.ComponentFromAssembly(
            application,
            ProductVariables.ToVar(KnownProductVariablesKeys.InstallDir),
            new ExtractorAdditionalInformation
            {
                OverrideFileName = ProductVariables.ToVar(ApplicationVariablesKeys.AppFileName),
            });

        var updaterAssembly = GetUpdaterAssemblyStream();
        var updaterComponent = MetadataExtractor.ComponentFromStream(
            updaterAssembly,
            ProductVariables.ToVar(ApplicationVariablesKeys.AppData), 
            new ExtractorAdditionalInformation
            {
                Drive = ExtractorAdditionalInformation.InstallDrive.System
            });

        var installables = new List<IInstallableComponent>
        {
            appComponent,
            updaterComponent
        };
        installables.AddRange(AdditionalComponents());

        var allComponents = installables.Cast<IProductComponent>().ToList();
        allComponents.Insert(0, CreateGroup(installedProduct, installables));

        return new ProductManifest(installedProduct, allComponents);
    }

    private static IComponentGroup CreateGroup(IProductReference product, IEnumerable<IInstallableComponent> componentInfos)
    {
        return new ComponentGroup(new ProductComponentIdentity(ApplicationConstants.AppGroupId, product.Version), componentInfos.ToList())
        {
            Name = product.Name
        };
    }

    protected virtual IEnumerable<IInstallableComponent> AdditionalComponents()
    {
        yield break;
    }

    private Stream GetUpdaterAssemblyStream()
    {
        var task = Task.Run(async () => await ResourceExtractor.GetResourceAsync(ExternalUpdaterConstants.AppUpdaterModuleName));
        task.Wait();
        return task.Result;
    }
}