using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.ExternalUpdater;

namespace AnakinRaW.ApplicationBase.Update;

internal class ApplicationProductService(IServiceProvider serviceProvider) : ProductServiceBase(serviceProvider)
{
    private readonly IApplicationEnvironment _applicationEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();
    private readonly IMetadataExtractor _metadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();
    private readonly IResourceExtractor _resourceExtractor = serviceProvider.GetRequiredService<IResourceExtractor>();

    private IDirectoryInfo? _installLocation;

    public override IDirectoryInfo InstallLocation => _installLocation ??= GetInstallLocation();

    protected override IProductReference CreateCurrentProductReference()
    {
        return _metadataExtractor.ProductReferenceFromAssembly(_applicationEnvironment.AssemblyInfo.Assembly);
    }

    protected override IProductManifest GetManifestForInstalledProduct(
        IProductReference installedProduct,
        IReadOnlyDictionary<string, string> productVariables)
    {
        var application = _applicationEnvironment.AssemblyInfo.Assembly;
        var appComponent = _metadataExtractor.ComponentFromAssembly(
            application,
            StringTemplateEngine.ToVariable(KnownProductVariablesKeys.InstallDir),
            new ExtractorAdditionalInformation
            {
                OverrideFileName = StringTemplateEngine.ToVariable(ApplicationVariablesKeys.AppFileName),
            });

        var updaterAssembly = GetUpdaterAssemblyStream();
        var updaterComponent = _metadataExtractor.ComponentFromStream(
            updaterAssembly,
            StringTemplateEngine.ToVariable(ApplicationVariablesKeys.AppData),
            new ExtractorAdditionalInformation
            {
                Drive = ExtractorAdditionalInformation.InstallDrive.System
            });

        var installables = new List<IInstallableComponent>
        {
            appComponent,
            updaterComponent
        };

        var allComponents = installables.Cast<IProductComponent>().ToList();
        allComponents.Insert(0, CreateGroup(installedProduct, installables));

        return new ProductManifest(installedProduct, allComponents);
    }

    protected override void AddAdditionalProductVariables(IDictionary<string, string> variables, IProductReference product)
    {
        variables.Add(ApplicationVariablesKeys.AppData, _applicationEnvironment.ApplicationLocalPath);
        variables.Add(ApplicationVariablesKeys.AppFileName, _applicationEnvironment.AssemblyInfo.ExecutableFileName);
    }

    private IDirectoryInfo GetInstallLocation()
    {
        var fs = ServiceProvider.GetRequiredService<IFileSystem>();
        var locationPath = _applicationEnvironment.AssemblyInfo.Assembly.Location;
        var directory = fs.FileInfo.New(locationPath).Directory;
        if (directory is null || !directory.Exists)
            throw new DirectoryNotFoundException("Unable to find location of current assembly.");
        return directory;
    }

    private static IComponentGroup CreateGroup(IProductReference product, IEnumerable<IInstallableComponent> componentInfos)
    {
        return new ComponentGroup(new ProductComponentIdentity(ApplicationConstants.AppGroupId, product.Version), componentInfos.ToList())
        {
            Name = product.Name
        };
    }

    private Stream GetUpdaterAssemblyStream()
    {
        var task = Task.Run(async () => await _resourceExtractor.GetResourceAsync(ExternalUpdaterConstants.AppUpdaterModuleName));
        task.Wait();
        return task.Result;
    }
}