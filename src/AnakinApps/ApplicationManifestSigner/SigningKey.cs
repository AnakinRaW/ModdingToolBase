using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace AnakinRaW.ApplicationManifestSigner;

/// <summary>
/// An ECDsa P-256 signing key paired with the certificate that holds its public half.
/// The caller is responsible for disposing this instance when finished with it.
/// </summary>
public sealed class SigningKey(ECDsa key, X509Certificate2 certificate) : IDisposable
{
    public ECDsa Key { get; } = key ?? throw new ArgumentNullException(nameof(key));
    public X509Certificate2 Certificate { get; } = certificate ?? throw new ArgumentNullException(nameof(certificate));

    public static SigningKey LoadFromPfx(string pfxPath, string? password)
    {
        if (pfxPath is null) throw new ArgumentNullException(nameof(pfxPath));
        var cert = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(pfxPath), password, X509KeyStorageFlags.Exportable);
        var ecdsa = cert.GetECDsaPrivateKey();
        if (ecdsa is null)
        {
            cert.Dispose();
            throw new InvalidOperationException("Pfx does not contain an ECDsa private key.");
        }
        return new SigningKey(ecdsa, cert);
    }

    public void Dispose()
    {
        Key.Dispose();
        Certificate.Dispose();
    }
}
