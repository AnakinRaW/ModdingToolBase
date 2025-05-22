using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Detection;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Manifest;

internal class ManifestInstallationDetector(IServiceProvider serviceProvider)
{
    private readonly IComponentInstallationDetector _installationDetector = serviceProvider.GetRequiredService<IComponentInstallationDetector>();

    public void DetectInstalledComponents(ProductManifest manifest, IReadOnlyDictionary<string, string> productVariables)
    {
        foreach (var manifestItem in manifest.GetInstallableComponents())
        {
            if (manifestItem.DetectedState == DetectionState.None)
                manifestItem.DetectedState = IsInstalled(manifestItem, productVariables);
        }
    }

    private DetectionState IsInstalled(InstallableComponent installable, IReadOnlyDictionary<string, string> productVariables)
    {
        return _installationDetector.IsInstalled(installable, productVariables)
            ? DetectionState.Present
            : DetectionState.Absent;
    }
}