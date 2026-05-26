using System;

namespace AnakinRaW.AppUpdaterFramework.Manifest;

/// <summary>
/// Represents a provider that delivers a specific instance of <see cref="ManifestLoaderBase"/> 
/// for managing product manifest operations, including parsing and verification.
/// </summary>
public sealed class ManifestLoaderProvider(ManifestLoaderBase loader) : IManifestLoaderProvider
{
    /// <inheritdoc/>
    public ManifestLoaderBase Loader { get; } = loader ?? throw new ArgumentNullException(nameof(loader));
}
