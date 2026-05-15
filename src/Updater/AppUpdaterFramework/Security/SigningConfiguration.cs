namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Configures the signature algorithm and enforcement policy used by the manifest signer and
/// verifier. Registered as a singleton by <c>AddUpdateFramework</c>; hosts override by registering
/// their own instance.
/// </summary>
public sealed class SigningConfiguration
{
    /// <summary>
    /// Default configuration: <see cref="SignatureAlgorithm.ES256"/> with <see cref="SignaturePolicy.Required"/>.
    /// </summary>
    public static readonly SigningConfiguration Default = new();

    /// <summary>
    /// Gets the signature algorithm the signer uses when producing a manifest's signature block. The
    /// verifier reads the algorithm per-manifest, so changing this does not invalidate older
    /// signatures.
    /// </summary>
    public SignatureAlgorithm SignatureAlgorithm { get; init; } = SignatureAlgorithm.ES256;

    /// <summary>
    /// Gets how strictly the framework's download/install pipeline enforces signature verification on
    /// downloaded manifests. Defaults to <see cref="SignaturePolicy.Required"/>.
    /// </summary>
    public SignaturePolicy Policy { get; init; } = SignaturePolicy.Required;
}
