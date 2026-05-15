using System.Text.Json.Serialization;

namespace AnakinRaW.AppUpdaterFramework.Json;

/// <summary>
/// Embedded signature block on a signed application manifest.
/// </summary>
/// <param name="Algorithm">JWS-style identifier of the signature/hash pair (e.g. <c>"ES256"</c>).</param>
/// <param name="Value">Base64-encoded signature over the algorithm's digest of the canonical manifest bytes.</param>
/// <param name="Certificate">Base64-encoded DER X.509 certificate that produced the signature.</param>
public record ManifestSignature(
    [property: JsonPropertyName("alg")] string Algorithm,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("cert")] string Certificate
);
