using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class UpdateCatalogProvider(IServiceProvider serviceProvider) : IUpdateCatalogProvider
{
    private readonly IManifestInstallationDetector _detector = serviceProvider.GetRequiredService<IManifestInstallationDetector>();

    public UpdateCatalog Create(InstalledProduct installedProduct, ProductManifest availableCatalog)
    {
        if (installedProduct == null) 
            throw new ArgumentNullException(nameof(installedProduct));
        if (availableCatalog == null)
            throw new ArgumentNullException(nameof(availableCatalog));

        var currentInstalledComponents = new HashSet<InstallableComponent>(installedProduct.Manifest.GetInstallableComponents(),
            ProductComponentIdentityComparer.VersionIndependent);

        var availableInstallableComponents = new HashSet<InstallableComponent>(availableCatalog.GetInstallableComponents(),
            ProductComponentIdentityComparer.VersionIndependent);

        if (!currentInstalledComponents.Any() && !availableInstallableComponents.Any())
            return new UpdateCatalog(installedProduct, availableCatalog.Product, []);

        // Empty available catalog: Uninstall
        if (!availableInstallableComponents.Any())
            return new UpdateCatalog(installedProduct, availableCatalog.Product, currentInstalledComponents
                .Select(c => new UpdateItem(c, null, UpdateAction.Delete)));

        // Empty current catalog: Fresh install
        if (!currentInstalledComponents.Any())
            return new UpdateCatalog(installedProduct, availableCatalog.Product, availableInstallableComponents
                    .Select(c => new UpdateItem(null, c, UpdateAction.Update)));


        _detector.DetectInstalledComponents(availableCatalog, installedProduct.Variables);

        var updateItems = Compare(currentInstalledComponents, availableInstallableComponents);
        return new UpdateCatalog(installedProduct, availableCatalog.Product, updateItems);
    }


    private static ICollection<UpdateItem> Compare(
        ICollection<InstallableComponent> installedComponents, 
        ICollection<InstallableComponent> availableComponents)
    {
        var updateItems = new List<UpdateItem>();

        var currentItems = installedComponents.ToList();

        foreach (var availableItem in availableComponents)
        {
            var installedComponent = currentItems.FirstOrDefault(c =>
                ProductComponentIdentityComparer.VersionIndependent.Equals(c, availableItem));


            var isDowngrade = false;
            if (installedComponent is not null)
            {
                currentItems.Remove(installedComponent);
                isDowngrade = availableItem.Version?.CompareSortOrderTo(installedComponent.Version) < 0;
            }

            UpdateAction action;
            if (availableItem.DetectedState == DetectionState.Present)
                action = UpdateAction.Keep;
            else
                action = !isDowngrade ? UpdateAction.Update : UpdateAction.Keep;
               
            updateItems.Add(new UpdateItem(installedComponent, availableItem, action));
        }

        foreach (var currentItem in currentItems)
            updateItems.Add(new UpdateItem(currentItem, null, UpdateAction.Delete));

        return updateItems;
    }
}