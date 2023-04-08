using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.ApplicationManifestCreator;

internal class ManifestCreator
{
    private readonly ILogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IMetadataExtractor _metadataExtractor;

    public ManifestCreatorOptions Options { get; }

    private JsonSerializerOptions JsonOptions { get; }

    public ManifestCreator(ManifestCreatorOptions options, IServiceProvider serviceProvider)
    {
        Requires.NotNull(options, nameof(options));
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _metadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();

        Options = options;
        JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public async Task<int> Run()
    {
        var manifest = await CreateManifest();
        await WriteManifest(manifest);
        return 0;
    }

    private async Task<ApplicationManifest> CreateManifest()
    {
        var installables = new HashSet<IInstallableComponent>(ProductComponentIdentityComparer.VersionAndBranchIndependent);

        await AddToComponents(Options.AppDataComponents, "TODO", installables);
        await AddToComponents(Options.InstallDirComponents, "TODO", installables);
        installables.Add(await _metadataExtractor.ComponentFromFile(_fileSystem.FileInfo.New(Options.ApplicationFile), "TODO"));


        var allComponents = installables.Cast<IProductComponent>().ToList();
        allComponents.Insert(0, CreateGroup(installables));
        
        var productReference = await _metadataExtractor.ProductReferenceFromFile(Options.ApplicationFile);

        return productReference.ToApplicationManifest(allComponents);
    }

    private async Task AddToComponents(IEnumerable<string> files, string installLocation, ISet<IInstallableComponent> set)
    {
        foreach (var component in files.Select(_fileSystem.FileInfo.New))
        {
            var installableComponent = await _metadataExtractor.ComponentFromFile(component, installLocation);
            if (!set.Add(installableComponent))
                throw new InvalidOperationException($"A duplicate component was created named: {installableComponent.Id}");
        }
    }

    private IComponentGroup CreateGroup(IEnumerable<IInstallableComponent> componentInfos)
    {
        // TODO: Fetch Group ID from somewhere
        return new ComponentGroup(new ProductComponentIdentity("ApplicationGroup"), componentInfos.ToList());
    }

    private async Task WriteManifest(ApplicationManifest manifest)
    {
        if (manifest == null)
            throw new ArgumentNullException(nameof(manifest));

        var outputFile = _fileSystem.FileInfo.New(Options.OuputFile);
        outputFile.Directory?.Create();

        _logger?.LogTrace($"Writing manifest to '{outputFile.FullName}'");

        await using var fileStream = outputFile.Create();
        await JsonSerializer.SerializeAsync(fileStream, manifest, JsonOptions);
    }
}