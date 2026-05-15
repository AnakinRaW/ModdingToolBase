using System;
using System.IO;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.Hashing;

namespace AnakinRaW.ApplicationManifestSigner;

/// <summary>
/// Signs a manifest with the supplied <see cref="SigningKey"/> and returns a copy with the
/// embedded signature block populated. Caller owns the lifetime of the key.
/// </summary>
public sealed class ManifestSigner(IHashingService hashingService, SigningConfiguration signingConfiguration)
{
    private readonly IHashingService _hashingService = hashingService ?? throw new ArgumentNullException(nameof(hashingService));
    private readonly SigningConfiguration _signingConfiguration = signingConfiguration ?? throw new ArgumentNullException(nameof(signingConfiguration));

    public ApplicationManifest Sign(ApplicationManifest manifest, SigningKey key)
    {
        if (manifest is null) 
            throw new ArgumentNullException(nameof(manifest));
        if (key is null) 
            throw new ArgumentNullException(nameof(key));

        var algorithm = _signingConfiguration.SignatureAlgorithm;
        var canonicalBytes = CanonicalManifestSerializer.SerializeForDigest(manifest);
        using var stream = new MemoryStream(canonicalBytes, writable: false);
        var digest = _hashingService.GetHash(stream, algorithm.GetHashType());

        var signatureBytes = key.Key.SignHash(digest);

        var signature = new ManifestSignature(
            Algorithm: algorithm.ToString(),
            Value: Convert.ToBase64String(signatureBytes),
            Certificate: Convert.ToBase64String(key.Certificate.RawData));

        return manifest with { Signature = signature };
    }
}
