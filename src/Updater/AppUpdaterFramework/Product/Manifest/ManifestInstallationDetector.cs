using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product.Detectors;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Product.Manifest;

internal class ManifestInstallationDetector(IServiceProvider serviceProvider) : IManifestInstallationDetector
{
    private readonly IComponentDetectorFactory _componentDetectorFactory = serviceProvider.GetService<IComponentDetectorFactory>() ?? ComponentDetectorFactory.Default;

    public IReadOnlyCollection<IInstallableComponent> DetectInstalledComponents(IProductManifest manifest, ProductVariables? productVariables = null)
    {
        if (manifest == null) 
            throw new ArgumentNullException(nameof(manifest));
        productVariables ??= new ProductVariables();

        var installedComponents = new HashSet<IInstallableComponent>(ProductComponentIdentityComparer.VersionIndependent);
        foreach (var manifestItem in manifest.Items)
        {
            if (manifestItem is not IInstallableComponent installable)
                continue;
            if (manifestItem.DetectedState == DetectionState.None)
                installable.DetectedState = IsInstalled(installable, productVariables);
            installedComponents.Add(installable);
        }
        return installedComponents.ToList();
    }

    private DetectionState IsInstalled(IInstallableComponent installable, ProductVariables productVariables)
    {
        var detector = _componentDetectorFactory.GetDetector(installable.Type, serviceProvider);
        return detector.GetCurrentInstalledState(installable, productVariables) ? DetectionState.Present : DetectionState.Absent;
    }
}