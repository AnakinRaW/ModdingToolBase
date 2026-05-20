using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.ExternalUpdater;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using Microsoft.Extensions.Logging;

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
        Logger?.LogInformation("Creating manifest for current installed product.");

        var installedComponents = new List<InstallableComponent>();

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
            Logger?.LogDebug("Creating component for external installer.");
            if (TryCreateExternalUpdaterComponent(productVariables, out var updaterComponent))
                installedComponents.Add(updaterComponent);
        }

        var productComponents = new List<ProductComponent>(installedComponents.Count + 1)
        {
            new ComponentGroup(ApplicationConstants.AppGroupId, installedProduct.Version, installedComponents)
            {
                Name = installedProduct.Name
            }
        };
        productComponents.AddRange(installedComponents);

        return new ProductManifest(installedProduct, productComponents);
    }

    // The installed manifest describes what is on disk.
    private bool TryCreateExternalUpdaterComponent(
        IReadOnlyDictionary<string, string> productVariables,
        [NotNullWhen(true)] out InstallableComponent? component)
    {
        var installDirectory = GetExternalUpdaterInstallLocation(productVariables, out var nextToApp);
        var resourceName = ExternalUpdaterConstants.GetExecutableFileName();
        var filePath = FileSystem.Path.Combine(installDirectory, resourceName);

        if (!FileSystem.File.Exists(filePath))
        {
            Logger?.LogDebug("External updater not present at '{Path}'", filePath);
            component = null;
            return false;
        }

        using var fileStream = FileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.Read);

        var componentInstallLocation =
            StringTemplateEngine.ToVariable(nextToApp
                ? KnownProductVariablesKeys.InstallDir
                : ApplicationVariablesKeys.AppData);

        component = _metadataExtractor.ComponentFromStream(
            fileStream,
            componentInstallLocation,
            new ExtractorAdditionalInformation
            {
                Drive = nextToApp
                    ? ExtractorAdditionalInformation.InstallDrive.App
                    : ExtractorAdditionalInformation.InstallDrive.System
            });
        return true;
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
        Logger?.LogTrace("Searching external updater at application location.");
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