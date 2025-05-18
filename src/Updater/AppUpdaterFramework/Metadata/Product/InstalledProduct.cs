using AnakinRaW.CommonUtilities;
using Semver;
using System;
using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

public sealed class InstalledProduct : ProductReference
{
    public string InstallationPath { get; }

    public IReadOnlyDictionary<string, string> Variables { get; }

    public ProductState State { get; internal set; }

    public ProductManifest Manifest { get; }

    internal InstalledProduct(
        string name, 
        SemVersion? version, 
        ProductBranch? branch,
        string installationPath, 
        ProductManifest manifest,
        IReadOnlyDictionary<string, string> variables,
        ProductState state = ProductState.Installed) : base(name, version, branch)
    {
        ThrowHelper.ThrowIfNullOrEmpty(installationPath);
        InstallationPath = installationPath;
        State = state;
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        Variables = variables ?? throw new ArgumentNullException(nameof(variables));
    }

    public override string ToString()
    {
        return $"Product '{Name}, v:{Version?.ToString() ?? "NO_VERSION"}, Branch:{Branch?.ToString() ?? "NO_BRANCH"}' " +
               $"at {InstallationPath}";
    }
}