using System;
using System.IO;
using AnakinRaW.AppUpdaterFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Security;

/// <summary>
/// Abstract base for manifest verifiers. The framework keys DI on this type — not on an
/// interface — so a host cannot register an unrelated implementation that bypasses signature
/// verification. The only host extension point is overriding <see cref="TryParseSignature"/> to
/// extract a <see cref="ParsedSignature"/> from the host's manifest format. <see cref="Verify"/>
/// is non-virtual and bakes in the policy short-circuit and crypto/trust chain.
/// </summary>
public abstract class ManifestVerifierBase
{
    private readonly ISignatureVerifier _signatureVerifier;
    private readonly IUpdateConfigurationProvider _configurationProvider;

    protected ManifestVerifierBase(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _signatureVerifier = serviceProvider.GetRequiredService<ISignatureVerifier>();
        _configurationProvider = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>();
    }

    /// <summary>
    /// Verifies a manifest stream against the configured signature policy. Returns
    /// <see cref="VerificationResult.Ok"/> immediately when policy is <see cref="SignaturePolicy.Off"/>;
    /// otherwise parses the signature block via <see cref="TryParseSignature"/> and runs the full
    /// crypto + trust check via <see cref="ISignatureVerifier"/>.
    /// </summary>
    public VerificationResult Verify(Stream manifestStream)
    {
        if (manifestStream is null)
            throw new ArgumentNullException(nameof(manifestStream));

        if (_configurationProvider.GetConfiguration().ManifestSigningConfiguration.Policy == SignaturePolicy.Off)
            return VerificationResult.Ok;

        var result = TryParseSignature(manifestStream, out var parsed);
        if (result != VerificationResult.Ok || parsed is null)
            return result;

        return _signatureVerifier.Verify(parsed);
    }

    /// <summary>
    /// Parses the manifest stream and produces the pieces needed for signature verification.
    /// </summary>
    /// <param name="manifestStream">The manifest content. The caller retains stream ownership.</param>
    /// <param name="parsed">
    /// On success, the populated <see cref="ParsedSignature"/>. On failure, <see langword="null"/>.
    /// </param>
    /// <returns>
    /// <see cref="VerificationResult.Ok"/> on success, otherwise the matching failure code
    /// (typically <see cref="VerificationResult.MissingSignature"/> or
    /// <see cref="VerificationResult.MalformedSignatureBlock"/>).
    /// </returns>
    protected abstract VerificationResult TryParseSignature(Stream manifestStream, out ParsedSignature? parsed);
}
