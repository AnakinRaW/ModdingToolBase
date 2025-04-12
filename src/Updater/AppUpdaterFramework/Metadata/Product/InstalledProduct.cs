using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.CommonUtilities;
using Semver;
using System;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

internal sealed class InstalledProduct : IInstalledProduct
{
    private readonly IProductReference _reference;

    public string Name => _reference.Name;

    public SemVersion? Version => _reference.Version;

    public ProductBranch? Branch => _reference.Branch;

    public string InstallationPath { get; }

    public IReadOnlyDictionary<string, string> Variables { get; }

    public ProductState State { get; internal set; }

    public IProductManifest Manifest { get; }

    public InstalledProduct(
        IProductReference reference, 
        string installationPath, 
        IProductManifest manifest,
        IReadOnlyDictionary<string, string> variables,
        ProductState state = ProductState.Installed)
    {
        ThrowHelper.ThrowIfNullOrEmpty(installationPath);
        _reference = reference ?? throw new ArgumentNullException(nameof(reference));
        InstallationPath = installationPath;
        State = state;
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        Variables = variables ?? throw new ArgumentNullException(nameof(variables));
    }

    public override string ToString()
    {
        return $"Product '{_reference.Name}, v:{_reference.Version?.ToString() ?? "NO_VERSION"}, Branch:{_reference.Branch?.ToString() ?? "NO_BRANCH"}' " +
               $"at {InstallationPath}";
    }
}