using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Security;

internal sealed class CertificateStore : ICertificateStore
{
    private static readonly HashTypeKey FingerprintHash = HashTypeKey.SHA256;
    
    private readonly IHashingService _hashingService;

    // Value byte is unused; ConcurrentDictionary gives us thread-safe set semantics.
    private readonly ConcurrentDictionary<string, byte> _fingerprints =
        new(StringComparer.OrdinalIgnoreCase);

    public CertificateStore(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
    }

    public void Add(X509Certificate2 certificate)
    {
        if (certificate is null)
            throw new ArgumentNullException(nameof(certificate));
        _fingerprints.TryAdd(Fingerprint(certificate), 0);
    }

    public bool Remove(X509Certificate2 certificate)
    {
        return certificate is null
            ? throw new ArgumentNullException(nameof(certificate)) 
            : _fingerprints.TryRemove(Fingerprint(certificate), out _);
    }

    public bool Contains(X509Certificate2 certificate)
    {
        return certificate is null 
            ? throw new ArgumentNullException(nameof(certificate))
            : _fingerprints.ContainsKey(Fingerprint(certificate));
    }

    private string Fingerprint(X509Certificate2 cert)
    {
        using var stream = new MemoryStream(cert.RawData, writable: false);
        var bytes = _hashingService.GetHash(stream, FingerprintHash);
        return Convert.ToBase64String(bytes);
    }
}
