using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Detection;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

internal class ManifestInstallationDetector(IServiceProvider serviceProvider) : IManifestInstallationDetector
{
    private readonly IComponentInstallationDetector _installationDetector = serviceProvider.GetRequiredService<IComponentInstallationDetector>();

    public void DetectInstalledComponents(ProductManifest manifest, IReadOnlyDictionary<string, string> productVariables)
    {
        if (manifest == null) 
            throw new ArgumentNullException(nameof(manifest));
        if (productVariables == null) 
            throw new ArgumentNullException(nameof(productVariables));

        foreach (var manifestItem in manifest.Components)
        {
            if (manifestItem is not IInstallableComponent installable)
                continue;
            if (manifestItem.DetectedState == DetectionState.None)
                installable.DetectedState = IsInstalled(installable, productVariables);
        }
    }

    private DetectionState IsInstalled(IInstallableComponent installable, IReadOnlyDictionary<string, string> productVariables)
    {
        return _installationDetector.IsInstalled(installable, productVariables)
            ? DetectionState.Present
            : DetectionState.Absent;
    }
}