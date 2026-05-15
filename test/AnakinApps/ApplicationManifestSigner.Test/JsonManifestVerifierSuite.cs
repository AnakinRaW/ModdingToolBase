using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Security.Testing;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationManifestSigner.Test;

/// <summary>
/// Runs <see cref="ManifestVerifierTestSuiteBase"/> against <see cref="JsonManifestVerifier"/>,
/// using <see cref="ManifestSigner"/> to produce signed manifests for the inherited tests.
/// </summary>
public class JsonManifestVerifierSuite : ManifestVerifierTestSuiteBase
{
    protected override IManifestVerifier CreateVerifier(IServiceProvider serviceProvider)
        => new JsonManifestVerifier(serviceProvider);

    protected override Stream CreateUnsignedManifestStream()
        => ToStream(TestManifests.CreateSample());

    protected override Stream CreateSignedManifestStream(ECDsa signingKey, X509Certificate2 signingCert, SignatureAlgorithm algorithm)
        => ToStream(SignWith(TestManifests.CreateSample(), signingKey, signingCert, algorithm));

    protected override Stream CreateTamperedContentStream(ECDsa signingKey, X509Certificate2 signingCert, SignatureAlgorithm algorithm)
    {
        var signed = SignWith(TestManifests.CreateSample(), signingKey, signingCert, algorithm);
        // Mutate a field after signing — recomputed digest differs, signature won't verify.
        return ToStream(signed with { Version = "9.9.9" });
    }

    protected override Stream CreateCorruptedSignatureBytesStream(ECDsa signingKey, X509Certificate2 signingCert, SignatureAlgorithm algorithm)
    {
        var signed = SignWith(TestManifests.CreateSample(), signingKey, signingCert, algorithm);
        // Flip the last byte of the signature value. Decode, mutate, re-encode.
        var sigBytes = Convert.FromBase64String(signed.Signature!.Value);
        sigBytes[^1] ^= 0xFF;
        var corrupted = signed with
        {
            Signature = signed.Signature with { Value = Convert.ToBase64String(sigBytes) }
        };
        return ToStream(corrupted);
    }

    protected override Stream CreateCertSubstitutedStream(ECDsa signingKey, X509Certificate2 signingCert, X509Certificate2 substituteCert, SignatureAlgorithm algorithm)
    {
        var signed = SignWith(TestManifests.CreateSample(), signingKey, signingCert, algorithm);
        // Replace the embedded cert with the substitute. Signature itself is unchanged so it still
        // verifies against signingKey's public key, but now the manifest claims it was signed by
        // substituteCert — the verifier uses the embedded cert, so VerifyHash fails.
        var substituted = signed with
        {
            Signature = signed.Signature! with { Certificate = Convert.ToBase64String(substituteCert.RawData) }
        };
        return ToStream(substituted);
    }

    private ApplicationManifest SignWith(ApplicationManifest manifest, ECDsa key, X509Certificate2 cert, SignatureAlgorithm algorithm)
    {
        var sp = new ServiceCollection()
            .AddSingleton(ServiceProvider.GetRequiredService<IHashingService>())
            .AddSingleton(new SigningConfiguration { SignatureAlgorithm = algorithm })
            .BuildServiceProvider();
        var signer = new ManifestSigner(sp);
        // Wrap without disposing: base suite owns the key/cert lifetime and still uses the cert
        // after this method returns.
        var signingKey = new SigningKey(key, cert);
        return signer.Sign(manifest, signingKey);
    }

    private static Stream ToStream(ApplicationManifest manifest)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, manifest, ManifestJsonOptions.Default);
        ms.Position = 0;
        return ms;
    }
}
