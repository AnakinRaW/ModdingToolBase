using System;
using AnakinRaW.CommonUtilities.Hashing;

namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Mapping helpers between <see cref="SignatureAlgorithm"/> and the concrete primitives used by
/// the signer and verifier.
/// </summary>
public static class SignatureAlgorithmExtensions
{
    /// <summary>
    /// Returns the <see cref="HashTypeKey"/> paired with <paramref name="algorithm"/>'s curve.
    /// </summary>
    /// <param name="algorithm">The signature algorithm.</param>
    /// <returns>The hash-type key the signer and verifier feed to <see cref="IHashingService"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"> <paramref name="algorithm"/> is not a defined enum value. </exception>
    public static HashTypeKey GetHashType(this SignatureAlgorithm algorithm) => algorithm switch
    {
        SignatureAlgorithm.ES256 => HashTypeKey.SHA256,
        SignatureAlgorithm.ES384 => HashTypeKey.SHA384,
        SignatureAlgorithm.ES512 => HashTypeKey.SHA512,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
    };

    /// <summary>
    /// Attempts to parse a JWS-style algorithm identifier (e.g. <c>"ES256"</c>) into a
    /// <see cref="SignatureAlgorithm"/> value.
    /// </summary>
    /// <param name="value">The identifier string from a manifest's signature block.</param>
    /// <param name="algorithm">
    /// When this method returns <see langword="true"/>, the parsed algorithm value; Otherwise, the
    /// default enum value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="value"/> matches a supported algorithm identifier;
    /// otherwise <see langword="false"/>. Unknown identifiers are not errors at the parse level — the
    /// verifier reports them as <see cref="VerificationResult.UnsupportedAlgorithm"/>.
    /// </returns>
    public static bool TryParse(string? value, out SignatureAlgorithm algorithm)
    {
        switch (value)
        {
            case "ES256": 
                algorithm = SignatureAlgorithm.ES256;
                return true;
            case "ES384": 
                algorithm = SignatureAlgorithm.ES384;
                return true;
            case "ES512": 
                algorithm = SignatureAlgorithm.ES512;
                return true;
            default:
                algorithm = default; 
                return false;
        }
    }
}