namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Represents a signature parsed from a manifest, containing all the information needed
/// to verify the signature against the manifest's canonical byte form.
/// </summary>
/// <param name="Algorithm">JWS-style algorithm identifier read from the manifest's signature block (e.g. <c>"ES256"</c>).</param>
/// <param name="SignatureValue">The signature bytes, already decoded from whatever encoding the manifest format uses.</param>
/// <param name="CertificateDer">The signing certificate in DER form, already decoded.</param>
/// <param name="CanonicalBytes">The canonical byte form of the manifest used as input to the signature digest.</param>
public sealed record ParsedSignature(string Algorithm, byte[] SignatureValue, byte[] CertificateDer, byte[] CanonicalBytes);
