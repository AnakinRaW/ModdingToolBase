using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.AppUpdaterFramework.Product;
using Microsoft.Extensions.DependencyInjection;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Json;

public sealed class JsonManifestLoader(IServiceProvider serviceProvider) : ManifestLoaderBase(serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override ProductManifest ParseManifestAndSignature(Stream manifestStream, out ParsedSignature? signature)
    {
        signature = null;

        ApplicationManifest appManifest;
        try
        {
            appManifest = JsonSerializer.Deserialize<ApplicationManifest>(manifestStream, ManifestJsonOptions.Default)
                ?? throw new ManifestException("Deserialized manifest was null.");
        }
        catch (JsonException ex)
        {
            throw new ManifestException("Failed to parse manifest JSON.", ex);
        }

        var manifest = new ProductManifest(BuildReference(appManifest), BuildCatalog(appManifest.Components));

        var sig = appManifest.Signature;
        if (sig is null)
            return manifest;

        if (string.IsNullOrEmpty(sig.Algorithm) || string.IsNullOrEmpty(sig.Value) || string.IsNullOrEmpty(sig.Certificate))
            throw new SignatureVerificationFailedException(VerificationResult.MalformedSignatureBlock);

        byte[] signatureBytes, certBytes;
        try
        {
            signatureBytes = Convert.FromBase64String(sig.Value);
            certBytes = Convert.FromBase64String(sig.Certificate);
        }
        catch (FormatException)
        {
            throw new SignatureVerificationFailedException(VerificationResult.MalformedSignatureBlock);
        }

        var canonicalBytes = CanonicalManifestSerializer.SerializeForDigest(appManifest);
        signature = new ParsedSignature(sig.Algorithm, signatureBytes, certBytes, canonicalBytes);
        return manifest;
    }

    private ProductReference BuildReference(ApplicationManifest applicationManifest)
    {
        SemVersion? version = null;
        if (applicationManifest.Version is not null)
            version = SemVersion.Parse(applicationManifest.Version, SemVersionStyles.Any);

        ProductBranch? branch = null;
        if (applicationManifest.Branch is not null)
        {
            var branchManager = _serviceProvider.GetRequiredService<IBranchManager>();
            branch = branchManager.GetBranchFromName(applicationManifest.Branch);
        }
        return new ProductReference(applicationManifest.Name, version, branch);
    }

    private static List<ProductComponent> BuildCatalog(IEnumerable<AppComponent> manifestComponents)
    {
        var catalog = new List<ProductComponent>();
        foreach (var manifestComponent in manifestComponents)
        {
            switch (manifestComponent.Type)
            {
                case ComponentType.File:
                    catalog.Add(manifestComponent.ToInstallable());
                    break;
                case ComponentType.Group:
                    catalog.Add(manifestComponent.ToGroup());
                    break;
                default:
                    throw new InvalidOperationException($"{manifestComponent.Type} is not supported.");
            }
        }
        return catalog;
    }
}
