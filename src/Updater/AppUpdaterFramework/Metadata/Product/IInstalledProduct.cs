using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

public interface IInstalledProduct : IProductReference
{ 
    string InstallationPath { get; }

    IReadOnlyDictionary<string, string> Variables { get; }

    ProductState State { get; }

    IProductManifest Manifest { get; }
}