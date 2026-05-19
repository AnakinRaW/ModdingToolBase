using System;
using System.IO;
using System.Linq;
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
            if (!ChainsToTrustAnchor(cert))
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

    // Portable to netstandard2.0 — `X509ChainTrustMode.CustomRootTrust` + `CustomTrustStore`
    // would be cleaner but is .NET 5+ only. Anchors go in ExtraStore,
    // AllowUnknownCertificateAuthority masks the "unknown CA" error, and the chain's terminal
    // cert is byte-checked against our anchors ourselves. The OS cert store may influence path
    // building but can't grant trust the terminal check rejects.
    private bool ChainsToTrustAnchor(X509Certificate2 cert)
    {
        var trustAnchors = _trustedCertificates.GetAll();
        if (trustAnchors.Count == 0)
            return false;

        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationTime = DateTime.UtcNow;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
        foreach (var anchor in trustAnchors)
            chain.ChainPolicy.ExtraStore.Add(anchor);

        if (!chain.Build(cert))
            return false;

        var terminal = chain.ChainElements[chain.ChainElements.Count - 1].Certificate.RawData;
        foreach (var anchor in trustAnchors)
        {
            if (anchor.RawData.SequenceEqual(terminal))
                return true;
        }
        return false;
    }
}
