using System.Diagnostics;
using System.IO;
using System;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase.Environment;
using AnakinRaW.ApplicationBase.Utilities;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.ExternalUpdater;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Services;

internal class ExternalUpdateExtractor : IExternalUpdateExtractor
{
    private readonly IResourceExtractor _resourceExtractor;
    private readonly IMetadataExtractor _metadataExtractor;
    private readonly ApplicationEnvironment _applicationEnvironment;

    public ExternalUpdateExtractor(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _metadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();
        _resourceExtractor = serviceProvider.GetRequiredService<IResourceExtractor>();
    }

    public async Task ExtractAsync()
    {
        await _resourceExtractor.ExtractAsync(
            ExternalUpdaterConstants.GetAssemblyFileName(),
            _applicationEnvironment.ApplicationLocalPath, 
            ShouldOverwriteUpdater);
    }

    private bool ShouldOverwriteUpdater(string filePath, Stream assemblyStream)
    {
        if (!Version.TryParse(FileVersionInfo.GetVersionInfo(filePath).FileVersion, out var installedVersion))
            return true;
        var streamVersion = _metadataExtractor.InformationFromStream(assemblyStream).FileVersion;
        if (streamVersion is null)
            return true;
        return streamVersion > installedVersion;
    }
}