using System;
using System.IO;

namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Verifies an application manifest's embedded signature against the host's
/// <see cref="ICertificateStore"/>.
/// </summary>
public interface IManifestVerifier
{
    /// <summary>
    /// Reads the manifest content from <paramref name="manifestStream"/>, extracts its embedded
    /// signature, and verifies that signature against the registered <see cref="ICertificateStore"/>.
    /// </summary>
    /// <param name="manifestStream">
    /// A readable stream positioned at the start of the manifest content. The implementation reads
    /// the stream to the end. The caller retains ownership of the stream.
    /// </param>
    /// <returns>
    /// <see cref="VerificationResult.Ok"/> if the signature is present, the signing certificate is
    /// trusted, and the cryptographic verification succeeds; otherwise the matching failure value.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="manifestStream"/> is <see langword="null"/>.</exception>
    VerificationResult Verify(Stream manifestStream);
}
