﻿using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Metadata.Update;
using AnakinRaW.AppUpdaterFramework.Product.Manifest;
using AnakinRaW.AppUpdaterFramework.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Updater;

internal class UpdateCatalogProvider(IServiceProvider serviceProvider) : IUpdateCatalogProvider
{
    private readonly IManifestInstallationDetector _detector = serviceProvider.GetRequiredService<IManifestInstallationDetector>();

    public IUpdateCatalog Create(IInstalledProduct installedProduct, IInstalledComponentsCatalog currentCatalog, IProductManifest availableCatalog)
    {
        if (currentCatalog == null)
            throw new ArgumentNullException(nameof(currentCatalog));
        if (availableCatalog == null)
            throw new ArgumentNullException(nameof(availableCatalog));

        if (!ProductReferenceEqualityComparer.NameOnly.Equals(currentCatalog.Product, availableCatalog.Product))
            throw new InvalidOperationException("Cannot build update catalog from different products.");

        var currentInstalledComponents = new HashSet<IInstallableComponent>(currentCatalog.GetInstallableComponents(),
            ProductComponentIdentityComparer.VersionIndependent);

        var availableInstallableComponents = new HashSet<IInstallableComponent>(availableCatalog.GetInstallableComponents(),
            ProductComponentIdentityComparer.VersionIndependent);

        if (!currentInstalledComponents.Any() && !availableInstallableComponents.Any())
            return UpdateCatalog.CreateEmpty(installedProduct, availableCatalog.Product);

        // Empty available catalog: Uninstall
        if (!availableInstallableComponents.Any())
            return new UpdateCatalog(installedProduct, availableCatalog.Product, currentInstalledComponents
                .Select(c => new UpdateItem(c, null, UpdateAction.Delete)), UpdateCatalogAction.Uninstall);

        // Empty current catalog: Fresh install
        if (!currentInstalledComponents.Any())
            return new UpdateCatalog(installedProduct, availableCatalog.Product, availableInstallableComponents
                    .Select(c => new UpdateItem(null, c, UpdateAction.Update)), UpdateCatalogAction.Install);


        var availableInstalledComponents =
            _detector.DetectInstalledComponents(availableCatalog, installedProduct.Variables);


        var updateItems = Compare(currentCatalog, availableInstalledComponents);

        var action = updateItems.Any(i => i.Action is UpdateAction.Delete or UpdateAction.Update)
            ? UpdateCatalogAction.Update
            : UpdateCatalogAction.None;

        return new UpdateCatalog(installedProduct, availableCatalog.Product, updateItems, action);
    }


    private static ICollection<IUpdateItem> Compare(IInstalledComponentsCatalog currentCatalog, IEnumerable<IInstallableComponent> availableComponents)
    {
        var updateItems = new List<IUpdateItem>();

        var currentItems = currentCatalog.Items.ToList();

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