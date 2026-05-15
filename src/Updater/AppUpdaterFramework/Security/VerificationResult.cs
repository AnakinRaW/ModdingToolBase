namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Outcome of a manifest signature verification performed by <see cref="IManifestVerifier"/>.
/// All non-<see cref="Ok"/> values are failure modes the host can pattern-match on.
/// </summary>
public enum VerificationResult
{
    /// <summary>
    /// The signature is present, the signing certificate is trusted by the
    /// <see cref="ICertificateStore"/>, and the signature is cryptographically valid over the
    /// canonical manifest bytes.
    /// </summary>
    Ok,

    /// <summary>
    /// The manifest was parsed successfully but contains no <c>signature</c> block. Returned only
    /// when <see cref="SigningConfiguration.Policy"/> is <see cref="SignaturePolicy.Required"/>.
    /// </summary>
    MissingSignature,

    /// <summary>
    /// The signature block is present but cannot be processed — required fields are empty, the
    /// base64-encoded values do not decode, the embedded certificate is not a valid X.509 cert,
    /// or the manifest JSON itself is malformed.
    /// </summary>
    MalformedSignatureBlock,

    /// <summary>
    /// The signature block names an algorithm identifier the verifier does not support. The
    /// signature itself was not evaluated.
    /// </summary>
    UnsupportedAlgorithm,

    /// <summary>
    /// The signing certificate is well-formed but is not in the host's <see cref="ICertificateStore"/>.
    /// The signature itself was not evaluated.
    /// </summary>
    UntrustedCert,

    /// <summary>
    /// The signature block is well-formed and the certificate is trusted, but the cryptographic
    /// signature does not verify over the canonical manifest bytes. The manifest has been altered
    /// since it was signed, or the signature value has been tampered with.
    /// </summary>
    SignatureInvalid,
}
