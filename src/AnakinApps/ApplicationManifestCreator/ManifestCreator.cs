using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AnakinRaW.ApplicationBase;
using AnakinRaW.AppUpdaterFramework.Json;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.ApplicationManifestCreator;

internal class ManifestCreator
{
    private readonly ILogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly AssemblyMetadataExtractor _metadataExtractor;
    private readonly AppManifestCreatorBranchManager _branchManager;

    public ManifestCreatorOptions Options { get; }

    private JsonSerializerOptions JsonOptions { get; }

    public ManifestCreator(ManifestCreatorOptions options, IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _metadataExtractor = new AssemblyMetadataExtractor(serviceProvider);
        _branchManager = serviceProvider.GetRequiredService<AppManifestCreatorBranchManager>();

        Options = options ?? throw new ArgumentNullException(nameof(options));
        JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<int> Run()
    {
        var branch = _branchManager.GetBranchFromName(Options.Branch ?? _branchManager.StableBranchName);
        var productReference = await _metadataExtractor.ProductReferenceFromFileAsync(_fileSystem.FileInfo.New(Options.ApplicationFile));

        productReference = new ProductReference(productReference.Name, productReference.Version, branch);
        
        var manifest = await CreateManifest(productReference);
        await WriteManifest(manifest);

        var currentOnlineBranches = (await _branchManager.GetAvailableBranchesAsync()).ToList();
        if (!currentOnlineBranches.Contains(branch)) 
            await WriteBranchesFile(currentOnlineBranches, branch);

        return 0;
    }

    private async Task WriteBranchesFile(IEnumerable<ProductBranch> currentOnlineBranches, ProductBranch newBranch)
    {
        var branches = currentOnlineBranches.Select(b => b.Name).ToHashSet();
        if (!branches.Add(newBranch.Name))
            return;

        var outputFilePath = _fileSystem.Path.Combine(Options.OutputPath, ApplicationConstants.BranchLookupFileName);
        var outputFile = _fileSystem.FileInfo.New(outputFilePath);
        outputFile.Directory?.Create();

        _logger?.LogTrace($"Writing branches lookup file to '{outputFile.FullName}'");
        await using var fileStream = outputFile.Create();
        await using var textWriter = new StreamWriter(fileStream);
        foreach (var branch in branches) 
            await textWriter.WriteLineAsync(branch);
    }

    private async Task<ApplicationManifest> CreateManifest(ProductReference productReference)
    {
        var branch = productReference.Branch;
        if (branch is null)
            throw new InvalidOperationException("No product newBranch created");

        var application = _fileSystem.FileInfo.New(Options.ApplicationFile);
        var appComponent = await _metadataExtractor.ComponentFromFileAsync(
            application,
            StringTemplateEngine.ToVariable(KnownProductVariablesKeys.InstallDir),
            new ExtractorAdditionalInformation
            {
                Drive = ExtractorAdditionalInformation.InstallDrive.App,
                OverrideFileName = StringTemplateEngine.ToVariable(ApplicationVariablesKeys.AppFileName),
                Origin = _branchManager.GetComponentOrigin(application, branch)
            });

        var installables =
            new HashSet<IInstallableComponent>(ProductComponentIdentityComparer.VersionIndependent)
            {
                appComponent
            };
        await AddToComponents(
            Options.InstallDirComponents,
            StringTemplateEngine.ToVariable(KnownProductVariablesKeys.InstallDir),
            ExtractorAdditionalInformation.InstallDrive.App,
            branch,
            installables);
        await AddToComponents(
            Options.AppDataComponents,
            StringTemplateEngine.ToVariable(ApplicationVariablesKeys.AppData),
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

    private static IComponentGroup CreateGroup(ProductReference product, IEnumerable<IInstallableComponent> componentInfos)
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

        var outputFilePath = _fileSystem.Path.Combine(Options.OutputPath, ApplicationConstants.ManifestFileName);
        var outputFile = _fileSystem.FileInfo.New(outputFilePath);
        outputFile.Directory?.Create();

        _logger?.LogTrace($"Writing manifest to '{outputFile.FullName}'");

        await using var fileStream = outputFile.Create();
        await JsonSerializer.SerializeAsync(fileStream, manifest, JsonOptions);
    }
}