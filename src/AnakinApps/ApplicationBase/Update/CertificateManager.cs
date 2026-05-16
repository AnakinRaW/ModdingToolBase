using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using AnakinRaW.AppUpdaterFramework.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Update;

/// <summary>
/// Registers trusted certificates into the framework's <see cref="ICertificateStore"/> from common
/// sources (raw bytes, embedded resources, files). Refuses any input that carries a private key —
/// only public DER certificates belong in a trust store.
/// </summary>
public sealed class CertificateManager
{
    private readonly ICertificateStore _store;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;

    public CertificateManager(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _store = serviceProvider.GetRequiredService<ICertificateStore>();
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<CertificateManager>();
    }

    /// <summary>
    /// Loads <paramref name="derBytes"/> as an X.509 certificate and adds it to the trust store.
    /// </summary>
    /// <param name="derBytes">DER-encoded certificate bytes.</param>
    /// <param name="source">Free-text description of the byte source, used in exception messages and logs.</param>
    /// <exception cref="InvalidOperationException"><paramref name="derBytes"/> decodes to a certificate that carries a private key.</exception>
    public void RegisterTrustedCertificate(byte[] derBytes, string source)
    {
        if (derBytes is null)
            throw new ArgumentNullException(nameof(derBytes));
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        using var cert = new X509Certificate2(derBytes);
        if (cert.HasPrivateKey)
            throw new InvalidOperationException(
                $"Trusted certificate from {source} carries a private key. " +
                "Only the public certificate (DER-encoded .cer) belongs in the trust store; " +
                "embedding a PFX would ship the signing key to every consumer.");

        _store.Add(cert);
        _logger?.LogDebug("Registered trusted certificate '{Subject}' from {Source}.", cert.Subject, source);
    }

    /// <summary>
    /// Loads a trust certificate from an embedded resource and registers it.
    /// </summary>
    /// <returns><see langword="true"/> if the resource existed and was registered; <see langword="false"/> if no such resource is in the assembly.</returns>
    public bool TryRegisterFromEmbeddedResource(Assembly assembly, string resourceName)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));
        if (resourceName is null)
            throw new ArgumentNullException(nameof(resourceName));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return false;

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        RegisterTrustedCertificate(ms.ToArray(), $"embedded resource '{resourceName}'");
        return true;
    }

    /// <summary>
    /// Loads a trust certificate from a file and registers it.
    /// </summary>
    /// <returns><see langword="true"/> if the file existed and was registered; <see langword="false"/> if the file is missing.</returns>
    public bool TryRegisterFromFile(string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));

        if (!_fileSystem.File.Exists(path))
            return false;

        RegisterTrustedCertificate(_fileSystem.File.ReadAllBytes(path), path);
        return true;
    }

    /// <summary>
    /// Registers trust certificates from the app's embedded resource and, when supplied, an
    /// additional file (typically a local-deploy / developer cert). Logs a warning when no
    /// certificate was registered from either source.
    /// </summary>
    /// <param name="assembly">Assembly to look up <paramref name="embeddedResourceName"/> in.</param>
    /// <param name="embeddedResourceName">Embedded resource name for the production trust cert.</param>
    /// <param name="developerCertFilePath">Optional path to a developer/local-deploy cert; pass <see langword="null"/> to skip.</param>
    public void RegisterTrustedCertificates(Assembly assembly, string embeddedResourceName, string? developerCertFilePath)
    {
        var anyRegistered = TryRegisterFromEmbeddedResource(assembly, embeddedResourceName);
        if (developerCertFilePath is not null)
            anyRegistered |= TryRegisterFromFile(developerCertFilePath);
        if (!anyRegistered)
            _logger?.LogWarning("No trusted certificates registered; signature verification will reject every manifest.");
    }
}
