namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Identifier for the combined signature-and-hash algorithm used to sign a manifest. Values follow
/// the JWS naming convention (RFC 7518): curve and hash paired in a single identifier.
/// </summary>
public enum SignatureAlgorithm
{
    /// <summary>ECDSA using P-256 and SHA-256.</summary>
    ES256,

    /// <summary>ECDSA using P-384 and SHA-384.</summary>
    ES384,

    /// <summary>ECDSA using P-521 and SHA-512.</summary>
    ES512,
}