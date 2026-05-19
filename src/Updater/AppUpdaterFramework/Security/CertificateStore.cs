using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Security;

internal sealed class CertificateStore : ICertificateStore, IDisposable
{
    private static readonly HashTypeKey FingerprintHash = HashTypeKey.SHA256;

    private readonly IHashingService _hashingService;

    private readonly ConcurrentDictionary<string, X509Certificate2> _certs =
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

        var fingerprint = Fingerprint(certificate);

        // Skip the allocation if we already trust this cert.
        if (_certs.ContainsKey(fingerprint))
            return;

        // Take our own copy of the cert bytes so the caller can dispose theirs freely.
        var owned = LoadFromDer(certificate.RawData);
        if (!_certs.TryAdd(fingerprint, owned))
        {
            // Lost the race — another thread added the same fingerprint first.
            owned.Dispose();
        }
    }

    public bool Remove(X509Certificate2 certificate)
    {
        if (certificate is null)
            throw new ArgumentNullException(nameof(certificate));

        if (_certs.TryRemove(Fingerprint(certificate), out var owned))
        {
            owned.Dispose();
            return true;
        }
        return false;
    }

    public bool Contains(X509Certificate2 certificate)
    {
        return certificate is null
            ? throw new ArgumentNullException(nameof(certificate))
            : _certs.ContainsKey(Fingerprint(certificate));
    }

    public IReadOnlyCollection<X509Certificate2> GetAll()
    {
        // ConcurrentDictionary.Values returns a moment-in-time snapshot; materialize it so the
        // caller gets a stable array regardless of subsequent Add/Remove on the store.
        return _certs.Values.ToArray();
    }

    public void Dispose()
    {
        // Snapshot keys to avoid iterating the live collection while removing from it.
        foreach (var fingerprint in _certs.Keys.ToArray())
        {
            if (_certs.TryRemove(fingerprint, out var owned))
                owned.Dispose();
        }
    }

    private string Fingerprint(X509Certificate2 cert)
    {
        using var stream = new MemoryStream(cert.RawData, writable: false);
        var bytes = _hashingService.GetHash(stream, FingerprintHash);
        return Convert.ToBase64String(bytes);
    }

    private static X509Certificate2 LoadFromDer(byte[] der)
    {
#if NET9_0_OR_GREATER
        return X509CertificateLoader.LoadCertificate(der);
#else
        return new X509Certificate2(der);
#endif
    }
}
