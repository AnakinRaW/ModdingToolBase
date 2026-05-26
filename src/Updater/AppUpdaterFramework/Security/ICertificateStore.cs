using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// A set of certificates that the host accepts as trust anchors for manifest signature verification.
/// </summary>
/// <remarks>
/// Registered as a singleton by <c>AddUpdateFramework</c>. Equality is by SHA-256 fingerprint of
/// the DER-encoded certificate. Implementations must be thread-safe.
/// </remarks>
public interface ICertificateStore
{
    /// <summary>
    /// Adds a certificate to the set of trusted anchors.
    /// </summary>
    /// <param name="certificate">
    /// The certificate to trust. The store takes its own copy of the certificate; the caller
    /// retains ownership of the passed <see cref="X509Certificate2"/> instance and is free to
    /// dispose it.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/>.</exception>
    void Add(X509Certificate2 certificate);

    /// <summary>
    /// Removes <paramref name="certificate"/> from the set of trusted anchors.
    /// </summary>
    /// <param name="certificate">The certificate to no longer trust.</param>
    /// <returns>
    /// <see langword="true"/> if the certificate was present in the store and was removed;
    /// <see langword="false"/> if it was not in the store.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/>.</exception>
    bool Remove(X509Certificate2 certificate);

    /// <summary>
    /// Determines whether <paramref name="certificate"/> is in the set of trusted anchors.
    /// </summary>
    /// <param name="certificate">The candidate certificate to look up.</param>
    /// <returns>
    /// <see langword="true"/> if a certificate with the same DER fingerprint is in the store;
    /// otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="certificate"/> is <see langword="null"/>.</exception>
    bool Contains(X509Certificate2 certificate);

    /// <summary>
    /// Returns a snapshot of all currently-trusted certificates.
    /// </summary>
    /// <remarks>
    /// Returned instances are owned by the store and must not be disposed by the caller. The
    /// snapshot is consistent at the moment of the call; concurrent modifications via
    /// <see cref="Add"/> / <see cref="Remove"/> after this returns are not reflected.
    /// </remarks>
    IReadOnlyCollection<X509Certificate2> GetAll();
}
