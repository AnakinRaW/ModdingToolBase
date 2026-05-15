using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Base class for <see cref="IManifestVerifier"/> implementations. Performs the format-agnostic
/// verification work; subclasses provide the format-specific extraction from the manifest stream.
/// </summary>
public abstract class ManifestVerifierBase : IManifestVerifier
{
    private readonly IHashingService _hashingService;
    private readonly ICertificateStore _trustedCertificates;

    protected ManifestVerifierBase(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
        _trustedCertificates = serviceProvider.GetRequiredService<ICertificateStore>();
    }

    /// <inheritdoc/>
    public VerificationResult Verify(Stream manifestStream)
    {
        if (manifestStream is null)
            throw new ArgumentNullException(nameof(manifestStream));

        var result = TryParseSignature(manifestStream, out var parsed);
        if (result != VerificationResult.Ok || parsed is null)
            return result;

        if (!SignatureAlgorithmExtensions.TryParse(parsed.Algorithm, out var algorithm))
            return VerificationResult.UnsupportedAlgorithm;

        X509Certificate2 cert;
        try
        {
            cert = new X509Certificate2(parsed.CertificateDer);
        }
        catch (CryptographicException)
        {
            return VerificationResult.MalformedSignatureBlock;
        }

        try
        {
            if (!_trustedCertificates.Contains(cert))
                return VerificationResult.UntrustedCert;

            using var stream = new MemoryStream(parsed.CanonicalBytes, writable: false);
            var digest = _hashingService.GetHash(stream, algorithm.GetHashType());

            using var publicKey = cert.GetECDsaPublicKey();
            if (publicKey is null)
                return VerificationResult.MalformedSignatureBlock;

            return publicKey.VerifyHash(digest, parsed.SignatureValue)
                ? VerificationResult.Ok
                : VerificationResult.SignatureInvalid;
        }
        finally
        {
            cert.Dispose();
        }
    }

    /// <summary>
    /// Parses the manifest stream and produces the pieces needed for signature verification.
    /// </summary>
    /// <param name="manifestStream">The manifest content. The caller retains stream ownership.</param>
    /// <param name="parsed">
    /// On success, the populated <see cref="ParsedSignature"/>. On failure, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see cref="VerificationResult.Ok"/> on success, otherwise the matching failure code
    /// (typically <see cref="VerificationResult.MissingSignature"/> or
    /// <see cref="VerificationResult.MalformedSignatureBlock"/>).
    /// </returns>
    protected abstract VerificationResult TryParseSignature(Stream manifestStream, out ParsedSignature? parsed);
}
