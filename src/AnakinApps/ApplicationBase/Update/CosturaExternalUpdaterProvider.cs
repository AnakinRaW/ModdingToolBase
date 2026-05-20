using System;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.External;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.CommonUtilities.Hashing;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationBase.Update;

internal sealed class CosturaExternalUpdaterProvider : IExternalUpdaterProvider
{
    private readonly ApplicationEnvironment _environment;
    private readonly IFileSystem _fileSystem;
    private readonly IHashingService _hashingService;
    private readonly CosturaResourceExtractor _resourceExtractor;
    private readonly AssemblyMetadataExtractor _metadataExtractor;
    private readonly ILogger? _logger;
    private readonly Lazy<ComponentIntegrityInformation> _integrity;

    public CosturaExternalUpdaterProvider(ApplicationEnvironment environment, IServiceProvider serviceProvider)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
        _resourceExtractor = new CosturaResourceExtractor(environment.AssemblyInfo.Assembly, serviceProvider);
        _metadataExtractor = new AssemblyMetadataExtractor(serviceProvider);
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(CosturaExternalUpdaterProvider));
        _integrity = new Lazy<ComponentIntegrityInformation>(ComputeIntegrity);
    }

    public ComponentIntegrityInformation GetIntegrity() => _integrity.Value;

    // Extracts the embedded updater and overwrites a stale on-disk copy whenever the running exe's bundled version is newer. 
    public void EnsureAvailable()
    {
        var resourceName = RequireEmbeddedResource();
        var installDirectory = _environment.ApplicationLocalPath;
        if (!_fileSystem.Directory.Exists(installDirectory))
            _fileSystem.Directory.CreateDirectory(installDirectory);

        _resourceExtractor.Extract(resourceName, installDirectory, ShouldOverwrite);
        return;

        // This prevents a "double update" case where a main-app upgrade replaces ModVerify.exe but leaves an
        // older updater on disk, forcing a second update cycle just to bring the updater forward.
        bool ShouldOverwrite(string file, Stream embeddedStream)
        {
            if (!Version.TryParse(FileVersionInfo.GetVersionInfo(file).FileVersion, out var installedVersion))
                return true;
            var embeddedVersion = _metadataExtractor.InformationFromStream(embeddedStream).FileVersion;
            if (embeddedVersion is null)
                return true;
            _logger?.LogDebug("External updater on disk: {Installed}; embedded: {Embedded}.", installedVersion, embeddedVersion);
            return embeddedVersion > installedVersion;
        }
    }

    private ComponentIntegrityInformation ComputeIntegrity()
    {
        var resourceName = RequireEmbeddedResource();
        using var stream = _resourceExtractor.GetResourceStreamAsync(resourceName).GetAwaiter().GetResult();
        var hash = _hashingService.GetHash(stream, HashTypeKey.SHA256);
        return new ComponentIntegrityInformation(hash, HashTypeKey.SHA256);
    }

    private string RequireEmbeddedResource()
    {
        var resourceName = ExternalUpdaterConstants.GetExecutableFileName();
        if (!_resourceExtractor.Contains(resourceName))
            throw new InvalidOperationException(
                $"The application is configured for self-update via Costura but the external updater ('{resourceName}') is not embedded as a resource.");
        return resourceName;
    }
}
