using System;
using System.IO;

namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Default <see cref="IManifestVerifier"/> registered when no format-specific verifier is wired up.
/// Under <see cref="SignaturePolicy.Off"/> the base short-circuits and this is never asked to parse.
/// Under <see cref="SignaturePolicy.Required"/> it reports <see cref="VerificationResult.MissingSignature"/>,
/// which surfaces as <see cref="SignatureVerificationFailedException"/> — a clear signal that the host
/// forgot to register a real verifier (e.g. <c>AddJsonManifestVerifier()</c>).
/// </summary>
internal sealed class NullManifestVerifier(IServiceProvider serviceProvider) : ManifestVerifierBase(serviceProvider)
{
    protected override VerificationResult TryParseSignature(Stream manifestStream, out ParsedSignature? parsed)
    {
        parsed = null;
        return VerificationResult.MissingSignature;
    }
}
