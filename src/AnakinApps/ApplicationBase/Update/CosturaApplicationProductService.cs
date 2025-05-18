using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.ExternalUpdater;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;

namespace AnakinRaW.ApplicationBase.Update;

public class CosturaApplicationProductService(ApplicationEnvironment applicationEnvironment, IServiceProvider serviceProvider) 
    : ApplicationProductService(applicationEnvironment, serviceProvider)
{
    private readonly AssemblyMetadataExtractor _metadataExtractor = new(serviceProvider);

    private readonly CosturaResourceExtractor _resourceExtractor = new(applicationEnvironment.AssemblyInfo.Assembly, serviceProvider);

    protected sealed override ProductManifest GetManifestForInstalledProduct(
        ProductReference installedProduct,
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
            var updaterComponent = CreateExternalUpdaterComponent(productVariables);
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

    private IInstallableComponent CreateExternalUpdaterComponent(IReadOnlyDictionary<string, string> productVariables)
    {
        var installDirectory = GetExternalUpdaterInstallLocation(productVariables, out var nextToApp);

        using var updaterStream = GetUpdaterAssemblyStream(installDirectory, nextToApp);

        updaterStream.Seek(0, SeekOrigin.Begin);

        var componentInstallLocation =
            StringTemplateEngine.ToVariable(nextToApp
                ? KnownProductVariablesKeys.InstallDir
                : ApplicationVariablesKeys.AppData);

        return _metadataExtractor.ComponentFromStream(
            updaterStream,
            componentInstallLocation,
            new ExtractorAdditionalInformation
            {
                Drive = nextToApp
                    ? ExtractorAdditionalInformation.InstallDrive.App
                    : ExtractorAdditionalInformation.InstallDrive.System
            });
    }

    private Stream GetUpdaterAssemblyStream(string installDirectory, bool nextToApp)
    {
        var resourceName = ExternalUpdaterConstants.GetExecutableFileName();

        if (!nextToApp) 
            _resourceExtractor.Extract(resourceName, installDirectory, ShouldOverwriteUpdater);

        var filePath = FileSystem.Path.Combine(installDirectory, resourceName);
        return FileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);

        bool ShouldOverwriteUpdater(string file, Stream assemblyStream)
        {
            if (!Version.TryParse(FileVersionInfo.GetVersionInfo(file).FileVersion, out var installedVersion))
                return true;
            var streamVersion = _metadataExtractor.InformationFromStream(assemblyStream).FileVersion;
            if (streamVersion is null)
                return true;
            return streamVersion > installedVersion;
        }
    }

    private string GetExternalUpdaterInstallLocation(IReadOnlyDictionary<string, string> productVariables, out bool nextToApp)
    {
        var externalUpdaterFileName = ExternalUpdaterConstants.GetExecutableFileName();

        if (_resourceExtractor.Contains(externalUpdaterFileName))
        {
            nextToApp = false;
            var appDataPath = StringTemplateEngine.ResolveVariables(StringTemplateEngine.ToVariable(ApplicationVariablesKeys.AppData), productVariables);
            return appDataPath;
        }
#if DEBUG
        var localFilePath = FileSystem.Path.Combine(InstallLocation.FullName, externalUpdaterFileName);
        if (FileSystem.File.Exists(localFilePath))
        {
            nextToApp = true;
            return InstallLocation.FullName;
        }
#endif
        throw new InvalidOperationException(
            $"The application does not reference {ExternalUpdaterConstants.ComponentIdentity}.");
    }
}