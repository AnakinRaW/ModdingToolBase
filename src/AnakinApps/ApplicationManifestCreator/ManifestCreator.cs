﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Validation;

namespace AnakinRaW.ApplicationManifestCreator;

internal class ManifestCreator
{
    private readonly ILogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IMetadataExtractor _metadataExtractor;
    private readonly AppCreatorBranchManager _branchManager;

    public ManifestCreatorOptions Options { get; }

    private JsonSerializerOptions JsonOptions { get; }

    public ManifestCreator(ManifestCreatorOptions options, IServiceProvider serviceProvider)
    {
        Requires.NotNull(options, nameof(options));
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _metadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();
        _branchManager = serviceProvider.GetRequiredService<AppCreatorBranchManager>();

        Options = options;
        JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
        var productReference = await _metadataExtractor.ProductReferenceFromFileAsync(_fileSystem.FileInfo.New(Options.ApplicationFile));

        var branch = productReference.Branch;
        if (branch is null)
            throw new InvalidOperationException("No product branch created");

        var application = _fileSystem.FileInfo.New(Options.ApplicationFile);
        var appComponent = await _metadataExtractor.ComponentFromFileAsync(
            application,
            ProductVariables.ToVar(KnownProductVariablesKeys.InstallDir),
            new ExtractorAdditionalInformation
            {
                Drive = ExtractorAdditionalInformation.InstallDrive.App,
                OverrideFileName = ProductVariables.ToVar(ApplicationVariablesKeys.AppFileName),
                Origin = _branchManager.GetComponentOrigin(application, branch)
            });

        var installables =
            new HashSet<IInstallableComponent>(ProductComponentIdentityComparer.VersionIndependent)
            {
                appComponent
            };
        await AddToComponents(
            Options.InstallDirComponents,
            ProductVariables.ToVar(KnownProductVariablesKeys.InstallDir),
            ExtractorAdditionalInformation.InstallDrive.App,
            branch,
            installables);
        await AddToComponents(
            Options.AppDataComponents, 
            ProductVariables.ToVar(ApplicationVariablesKeys.AppData),
            ExtractorAdditionalInformation.InstallDrive.System,
            branch,
            installables);

        var allComponents = installables.Cast<IProductComponent>().ToList();
        allComponents.Insert(0, CreateGroup(productReference, installables));
        
       return productReference.ToApplicationManifest(allComponents);
    }

    private async Task AddToComponents(
        IEnumerable<string> files, 
        string installLocation, 
        ExtractorAdditionalInformation.InstallDrive drive, 
        ProductBranch branch,
        ISet<IInstallableComponent> set)
    {
        foreach (var component in files.Select(_fileSystem.FileInfo.New))
        {
            if (!component.Exists)
                throw new FileNotFoundException("Could not find component file:", component.FullName);
            var installableComponent = await _metadataExtractor.ComponentFromFileAsync(component, installLocation, new ExtractorAdditionalInformation
            {
                Drive = drive,
                Origin = _branchManager.GetComponentOrigin(component, branch)
            });


            if (!set.Add(installableComponent))
                throw new InvalidOperationException($"A duplicate component was created named: {installableComponent.Id}");
        }
    }

    private static IComponentGroup CreateGroup(IProductReference product, IEnumerable<IInstallableComponent> componentInfos)
    {
        return new ComponentGroup(new ProductComponentIdentity(ApplicationConstants.AppGroupId, product.Version), componentInfos.ToList())
        {
            Name = product.Name
        };
    }

    private async Task WriteManifest(ApplicationManifest manifest)
    {
        if (manifest == null)
            throw new ArgumentNullException(nameof(manifest));

        var outputFilePath = _fileSystem.Path.Combine(Options.OuputPath, ApplicationConstants.ManifestFileName);
        var outputFile = _fileSystem.FileInfo.New(outputFilePath);
        outputFile.Directory?.Create();

        _logger?.LogTrace($"Writing manifest to '{outputFile.FullName}'");

        await using var fileStream = outputFile.Create();
        await JsonSerializer.SerializeAsync(fileStream, manifest, JsonOptions);
    }
}