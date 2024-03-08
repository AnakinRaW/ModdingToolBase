using System;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.CommonUtilities;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

internal sealed class InstalledProduct : IInstalledProduct
{
    private readonly IProductReference _reference;

    public string Name => _reference.Name;

    public SemVersion? Version => _reference.Version;

    public ProductBranch? Branch => _reference.Branch;

    public string InstallationPath { get; }

    public ProductVariables Variables { get; }

    public ProductState State { get; internal set; }

    public IProductManifest Manifest { get; }

    public InstalledProduct(IProductReference reference, string installationPath, IProductManifest manifest, ProductVariables? variables, ProductState state = ProductState.Installed)
    {
        if (reference == null) 
            throw new ArgumentNullException(nameof(reference));
        if (manifest == null) 
            throw new ArgumentNullException(nameof(manifest));
        ThrowHelper.ThrowIfNullOrEmpty(installationPath);
        _reference = reference;
        InstallationPath = installationPath;
        State = state;
        Manifest = manifest;
        Variables = variables ?? new ProductVariables();
    }

    public override string ToString()
    {
        return $"Product '{_reference.Name}, v:{_reference.Version?.ToString() ?? "NO_VERSION"}, Branch:{_reference.Branch?.ToString() ?? "NO_BRANCH"}' " +
               $"at {InstallationPath}";
    }
}