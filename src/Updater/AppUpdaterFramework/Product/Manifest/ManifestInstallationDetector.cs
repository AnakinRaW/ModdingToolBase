using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Detection;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

internal class ManifestInstallationDetector(IServiceProvider serviceProvider) : IManifestInstallationDetector
{
    private readonly IComponentInstallationDetector _installationDetector = serviceProvider.GetRequiredService<IComponentInstallationDetector>();

    public IReadOnlyCollection<IInstallableComponent> DetectInstalledComponents(IProductManifest manifest, IReadOnlyDictionary<string, string> productVariables)
    {
        if (manifest == null) 
            throw new ArgumentNullException(nameof(manifest));
        if (productVariables == null) 
            throw new ArgumentNullException(nameof(productVariables));

        var installedComponents = new HashSet<IInstallableComponent>(ProductComponentIdentityComparer.VersionIndependent);
        foreach (var manifestItem in manifest.Components)
        {
            if (manifestItem is not IInstallableComponent installable)
                continue;
            if (manifestItem.DetectedState == DetectionState.None)
                installable.DetectedState = IsInstalled(installable, productVariables);
            installedComponents.Add(installable);
        }
        return installedComponents.ToList();
    }

    private DetectionState IsInstalled(IInstallableComponent installable, IReadOnlyDictionary<string, string> productVariables)
    {
        return _installationDetector.IsInstalled(installable, productVariables)
            ? DetectionState.Present
            : DetectionState.Absent;
    }
}