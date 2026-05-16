namespace AnakinRaW.AppUpdaterFramework.Manifest;

/// <summary>
/// Defines a provider for accessing a manifest loader, which is responsible for 
/// handling operations related to the verification and parsing of product manifests.
/// </summary>
public interface IManifestLoaderProvider
{
    /// <summary>
    /// Gets the manifest loader instance that is responsible for handling operations related to
    /// the verification and parsing of product manifests.
    /// </summary>
    ManifestLoaderBase Loader { get; }
}
