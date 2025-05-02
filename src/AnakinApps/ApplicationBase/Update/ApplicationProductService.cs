using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Update;

public abstract class ApplicationProductService(ApplicationEnvironment applicationEnvironment, IServiceProvider serviceProvider)
    : ProductServiceBase(serviceProvider)
{
    private readonly IMetadataExtractor _metadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();

    protected readonly ApplicationEnvironment ApplicationEnvironment = applicationEnvironment ?? throw new ArgumentNullException(nameof(applicationEnvironment));
    protected readonly IFileSystem FileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    [field: AllowNull, MaybeNull]
    public sealed override IDirectoryInfo InstallLocation => field ?? GetInstallLocation();
    
    protected sealed override IProductReference CreateCurrentProductReference()
    {
        return _metadataExtractor.ProductReferenceFromAssembly(ApplicationEnvironment.AssemblyInfo.Assembly);
    }

    protected override void AddAdditionalProductVariables(IDictionary<string, string> variables, IProductReference product)
    {
        variables.Add(ApplicationVariablesKeys.AppData, ApplicationEnvironment.ApplicationLocalPath);
        variables.Add(ApplicationVariablesKeys.AppFileName, ApplicationEnvironment.AssemblyInfo.ExecutableFileName);
    }

    private IDirectoryInfo GetInstallLocation()
    {
        var fs = ServiceProvider.GetRequiredService<IFileSystem>();
        var locationPath = ApplicationEnvironment.AssemblyInfo.Assembly.Location;
        var directory = fs.FileInfo.New(locationPath).Directory;
        if (directory is null || !directory.Exists)
            throw new DirectoryNotFoundException("Unable to find location of current assembly.");
        return directory;
    }
}


public class CosturaApplicationProductService(ApplicationEnvironment applicationEnvironment, IServiceProvider serviceProvider) 
    : ApplicationProductService(applicationEnvironment, serviceProvider)
{
    private readonly IMetadataExtractor _metadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();

    private readonly CosturaResourceExtractor _resourceExtractor = new(applicationEnvironment.AssemblyInfo.Assembly, serviceProvider);

    protected sealed override IProductManifest GetManifestForInstalledProduct(
        IProductReference installedProduct,
        IReadOnlyDictionary<string, string> productVariables)
    {
        var installedComponents = new List<IInstallableComponent>();

        var application = ApplicationEnvironment.AssemblyInfo.Assembly;
        var appComponent = _metadataExtractor.ComponentFromAssembly(
            application,
            StringTemplateEngine.ToVariable(KnownProductVariablesKeys.InstallDir),
            new ExtractorAdditionalInformation
            {
                OverrideFileName = StringTemplateEngine.ToVariable(ApplicationVariablesKeys.AppFileName),
            });

        installedComponents.Add(appComponent);

        if (ApplicationEnvironment is UpdatableApplicationEnvironment { UpdateConfiguration.RestartConfiguration.SupportsRestart: true })
        {
            var updaterComponent = CreateExternalUpdaterComponent();
            installedComponents.Add(updaterComponent);
        }

        var productComponents = new List<IProductComponent>(installedComponents.Count + 1)
        {
            new ComponentGroup(new ProductComponentIdentity(ApplicationConstants.AppGroupId, installedProduct.Version), installedComponents)
            {
                Name = installedProduct.Name
            }
        };
        productComponents.AddRange(installedComponents);

        return new ProductManifest(installedProduct, productComponents);
    }

    private IInstallableComponent CreateExternalUpdaterComponent()
    {
        using var updaterAssemblyStream = GetUpdaterAssemblyStream(out var overwriteLocation);

        var installLocation = overwriteLocation ?? StringTemplateEngine.ToVariable(ApplicationVariablesKeys.AppData);

        if (overwriteLocation is null)
        {
            //ExtractAssemblyToLocation(
            //    updaterAssemblyStream, 
            //    StringTemplateEngine.ResolveVariables(ApplicationVariablesKeys.AppData, productVariables));
        }

        updaterAssemblyStream.Seek(0, SeekOrigin.Begin);

        return _metadataExtractor.ComponentFromStream(
            updaterAssemblyStream,
            installLocation,
            new ExtractorAdditionalInformation
            {
                Drive = overwriteLocation is null
                    ? ExtractorAdditionalInformation.InstallDrive.System
                    : ExtractorAdditionalInformation.InstallDrive.App
            });
    }

    private Stream GetUpdaterAssemblyStream(out string? overwriteLocation)
    {
        overwriteLocation = null;
        try
        {
            var task = Task.Run(async () => await _resourceExtractor.GetResourceAsync(ExternalUpdaterConstants.GetAssemblyFileName()));
            task.Wait();
            return task.Result;
        }
#if DEBUG
        catch (AggregateException e)
        {
            if (e.GetBaseException() is not IOException)
                throw;

            Logger?.LogWarning(e, $"Unable to extract ExternalUpdater from embedded resources: {e.Message}");
            return ExternalUpdaterStreamFromLocal(out overwriteLocation);
        }
        catch (IOException e)
        {
            Logger?.LogWarning(e, $"Unable to extract ExternalUpdater from embedded resources: {e.Message}");
            return ExternalUpdaterStreamFromLocal(out overwriteLocation);
        }
#endif
        catch (Exception e)
        {
            Logger?.LogWarning(e, $"Unable to extract ExternalUpdater from embedded resources: {e.Message}");
            throw;
        }
    }

    private Stream ExternalUpdaterStreamFromLocal(out string location)
    {
        var fs = ServiceProvider.GetRequiredService<IFileSystem>();
        var file = fs.Path.Combine(InstallLocation.FullName, ExternalUpdaterConstants.GetAssemblyFileName());
        location = fs.Path.GetDirectoryName(file)!;
        return fs.FileStream.New(file, FileMode.Open, FileAccess.Read);
    }
}