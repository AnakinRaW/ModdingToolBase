using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

public interface IInstalledProduct : IProductReference
{ 
    string InstallationPath { get; }

    IReadOnlyDictionary<string, string> Variables { get; }

    ProductState State { get; }

    ProductManifest Manifest { get; }
}