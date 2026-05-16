using System;
using System.IO;
using System.Text.Json;
using AnakinRaW.AppUpdaterFramework.Security;

namespace AnakinRaW.AppUpdaterFramework.Json;

/// <summary>
/// JSON-schema <see cref="ManifestVerifierBase"/> subclass. Reads the manifest stream as
/// <see cref="ApplicationManifest"/> and surfaces its signature block to the base for cryptographic
/// verification.
/// </summary>
public sealed class JsonManifestVerifier(IServiceProvider serviceProvider) : ManifestVerifierBase(serviceProvider)
{
    protected override VerificationResult TryParseSignature(Stream manifestStream, out ParsedSignature? parsed)
    {
        parsed = null;

        ApplicationManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ApplicationManifest>(manifestStream, ManifestJsonOptions.Default);
        }
        catch (JsonException)
        {
            return VerificationResult.MalformedSignatureBlock;
        }
        if (manifest is null)
            return VerificationResult.MalformedSignatureBlock;

        var sig = manifest.Signature;
        if (sig is null)
            return VerificationResult.MissingSignature;

        if (string.IsNullOrEmpty(sig.Algorithm) || string.IsNullOrEmpty(sig.Value) || string.IsNullOrEmpty(sig.Certificate))
            return VerificationResult.MalformedSignatureBlock;

        byte[] signatureBytes, certBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(sig.Value);
            certBytes = Convert.FromBase64String(sig.Certificate);
        }
        catch (FormatException)
        {
            return VerificationResult.MalformedSignatureBlock;
        }

        var canonicalBytes = CanonicalManifestSerializer.SerializeForDigest(manifest);
        parsed = new ParsedSignature(sig.Algorithm, signatureBytes, certBytes, canonicalBytes);
        return VerificationResult.Ok;
    }
}
