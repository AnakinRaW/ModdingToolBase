using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Manifest;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Restart;
using AnakinRaW.AppUpdaterFramework.Security;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.External;

/// <summary>
/// Decides which integrity hashes the on-disk external updater may match before it is launched
/// (<see cref="IExternalUpdaterIntegrityCheck"/> performs the match itself).
/// </summary>
internal sealed class ExternalUpdaterTrustList(
    IExternalUpdaterProvider updaterProvider,
    IReadOnlyPendingUpdate pendingState,
    ManifestLoaderBase manifestLoader,
    UpdateConfiguration updateConfig,
    ILogger? logger = null)
{
    private readonly IExternalUpdaterProvider _updaterProvider = updaterProvider ?? throw new ArgumentNullException(nameof(updaterProvider));
    private readonly IReadOnlyPendingUpdate _pendingState = pendingState ?? throw new ArgumentNullException(nameof(pendingState));
    private readonly ManifestLoaderBase _manifestLoader = manifestLoader ?? throw new ArgumentNullException(nameof(manifestLoader));
    private readonly UpdateConfiguration _updateConfig = updateConfig ?? throw new ArgumentNullException(nameof(updateConfig));
    private readonly ILogger? _logger = logger;

    public IReadOnlyCollection<ComponentIntegrityInformation> GetTrustedIntegrityInformation()
    {
        var acceptable = new List<ComponentIntegrityInformation>();

        // The updater this process shipped with.
        var embedded = _updaterProvider.GetIntegrity();
        if (embedded.HashType == HashTypeKey.None || embedded.Hash is null)
            throw new InvalidOperationException("Unable to get integrity information for external updater.");
        acceptable.Add(embedded);

        // The hash the signed manifest declares. The updater lives in [AppData], is rarely locked,
        // and so installs in-process without entering PendingComponents — meaning a freshly installed
        // updater that differs from the embedded copy (newer build, same-version rebuild) is only
        // trustable from here. Omitting this rejects legitimate updaters and breaks self-update.
        var manifestIntegrity = TryGetUpdaterIntegrityFromFetchedManifest();
        if (HasHash(manifestIntegrity))
            acceptable.Add(manifestIntegrity!.Value);

        // The rarer case: the updater was locked and deferred to restart.
        var pendingComponent = _pendingState.PendingComponents
            .Select(p => p.Component)
            .OfType<SingleFileComponent>()
            .FirstOrDefault(c => c.Id == ExternalUpdaterConstants.ComponentIdentity);

        if (pendingComponent is not null)
        {
            var pendingIntegrity = pendingComponent.OriginInfo?.IntegrityInformation ?? ComponentIntegrityInformation.None;

            if (_updateConfig.ManifestSigningConfiguration.Policy == SignaturePolicy.Required && !HasHash(pendingIntegrity))
                throw new InvalidOperationException(
                    "Update security policy requires signing, but the pending external updater component has no integrity declaration. The signed-manifest pipeline is expected to populate it.");

            if (HasHash(pendingIntegrity))
                acceptable.Add(pendingIntegrity);
        }

        return acceptable;
    }

    private ComponentIntegrityInformation? TryGetUpdaterIntegrityFromFetchedManifest()
    {
        var bytes = _pendingState.FetchedManifestBytes;
        if (bytes is null || bytes.Length == 0)
            return null;

        try
        {
            using var stream = new MemoryStream(bytes, writable: false);
            // Re-verify so a Required policy only ever trusts a hash from a properly signed manifest.
            var manifest = _manifestLoader.LoadAndVerifyManifest(stream);

            var updater = manifest.Components
                .OfType<SingleFileComponent>()
                .FirstOrDefault(c => c.Id == ExternalUpdaterConstants.ComponentIdentity);

            return updater?.OriginInfo?.IntegrityInformation;
        }
        catch (Exception ex)
        {
            // Other sources still apply; the check fails safe if nothing matches.
            _logger?.LogWarning(ex, "Failed to derive external-updater integrity from the fetched manifest: {Message}", ex.Message);
            return null;
        }
    }

    private static bool HasHash(ComponentIntegrityInformation? integrity)
    {
        return integrity is { } i && i.HashType != HashTypeKey.None && i.Hash is not null;
    }
}
