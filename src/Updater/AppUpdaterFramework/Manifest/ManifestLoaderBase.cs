using System;
using System.IO;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Manifest;

/// <summary>
/// Abstract base for format-specific manifest loaders.
/// </summary>
public abstract class ManifestLoaderBase
{
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly IUpdateConfigurationProvider _configurationProvider;

    protected ManifestLoaderBase(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));
        
        _signatureVerifier = serviceProvider.GetRequiredService<ISignatureVerifier>();
        _configurationProvider = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>();
    }

    /// <summary>
    /// Parses the manifest stream and, when <see cref="SignaturePolicy.Required"/> is in effect,
    /// verifies the embedded signature against the configured <see cref="ICertificateStore"/>.
    /// Under <see cref="SignaturePolicy.Off"/> the signature block is ignored entirely and the
    /// parsed manifest is returned as-is, provided the body parsed and the signature block was
    /// either absent or well-formed.
    /// </summary>
    /// <param name="manifestStream">The downloaded manifest content.</param>
    /// <returns>The parsed manifest if verification succeeds (or policy is <see cref="SignaturePolicy.Off"/>).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="manifestStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ManifestException">The manifest body itself could not be parsed, or the subclass returned a null manifest.</exception>
    /// <exception cref="SignatureVerificationFailedException">
    /// The signature block was present-but-malformed (<see cref="VerificationResult.MalformedSignatureBlock"/>),
    /// missing under <see cref="SignaturePolicy.Required"/> (<see cref="VerificationResult.MissingSignature"/>),
    /// or failed cryptographic verification against the trust store
    /// (<see cref="VerificationResult.UnsupportedAlgorithm"/>, <see cref="VerificationResult.UntrustedCert"/>,
    /// <see cref="VerificationResult.SignatureInvalid"/>). The exception's <c>Result</c> property
    /// carries the specific failure code.
    /// </exception>
    public ProductManifest LoadAndVerifyManifest(Stream manifestStream)
    {
        if (manifestStream is null)
            throw new ArgumentNullException(nameof(manifestStream));

        var manifest = ParseManifestAndSignature(manifestStream, out var signature)
            ?? throw new ManifestException("Manifest loader returned a null product manifest.");

        var policy = _configurationProvider.GetConfiguration().ManifestSigningConfiguration.Policy;
        if (policy == SignaturePolicy.Off)
            return manifest;

        if (signature is null)
            throw new SignatureVerificationFailedException(VerificationResult.MissingSignature);

        var verifyResult = _signatureVerifier.Verify(signature);
        return verifyResult != VerificationResult.Ok
            ? throw new SignatureVerificationFailedException(verifyResult)
            : manifest;
    }

    /// <summary>
    /// Parses <paramref name="manifestStream"/> into a <see cref="ProductManifest"/> and extracts
    /// the embedded signature block, if any.
    /// </summary>
    /// <param name="manifestStream">The manifest content.</param>
    /// <param name="signature">The signature block, or <see langword="null"/> when the manifest is unsigned.</param>
    /// <returns>The parsed manifest.</returns>
    /// <exception cref="ManifestException">The manifest body could not be parsed.</exception>
    /// <exception cref="SignatureVerificationFailedException">
    /// The signature block is present but malformed.
    /// </exception>
    protected abstract ProductManifest ParseManifestAndSignature(
        Stream manifestStream,
        out ParsedSignature? signature);
}
