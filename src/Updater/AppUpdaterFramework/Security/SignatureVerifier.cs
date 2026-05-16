using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Security;

internal sealed class SignatureVerifier : ISignatureVerifier
{
    private readonly IHashingService _hashingService;
    private readonly ICertificateStore _trustedCertificates;

    public SignatureVerifier(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
        _trustedCertificates = serviceProvider.GetRequiredService<ICertificateStore>();
    }

    public VerificationResult Verify(ParsedSignature parsed)
    {
        if (parsed is null) 
            throw new ArgumentNullException(nameof(parsed));

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

        using (cert)
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
    }
}
