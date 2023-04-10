using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Conditions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Semver;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

public sealed class MetadataExtractor : IMetadataExtractor
{
    private const HashType FileHashType = HashType.Sha256;

    private readonly IFileSystem _fileSystem;
    private readonly IBranchManager _branchManager;
    private readonly IHashingService _hashingService;

    public MetadataExtractor(IServiceProvider serviceProvider)
    {
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
        _branchManager = serviceProvider.GetRequiredService<IBranchManager>();
    }

    public IInstallableComponent ComponentFromAssembly(Assembly assembly, string installLocation,
        ExtractorAdditionalInformation additionalInformation = default)
    {
        Requires.NotNull(assembly, nameof(assembly));
        Requires.NotNullOrEmpty(installLocation, nameof(installLocation));

        var assemblyFile = assembly.Location;
        using var assemblyStream = _fileSystem.FileStream.New(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return ComponentFromStream(assemblyStream, installLocation, additionalInformation);
    }

    public Task<IInstallableComponent> ComponentFromFileAsync(IFileInfo file, string installLocation,
        ExtractorAdditionalInformation additionalInformation = default)
    {
        Requires.NotNull(file, nameof(file));
        Requires.NotNullOrEmpty(installLocation, nameof(installLocation));

        if (!file.Exists)
            throw new FileNotFoundException("Component assembly not found.", file.FullName);

        return Task.Run(() =>
        {
            using var assemblyStream = file.OpenRead();
            return ComponentFromStream(assemblyStream, installLocation, additionalInformation);
        });
    }

    public IInstallableComponent ComponentFromStream(Stream stream, string installLocation,
        ExtractorAdditionalInformation additionalInformation = default)
    {
        Requires.NotNull(stream, nameof(stream));
        Requires.NotNullOrEmpty(installLocation, nameof(installLocation));

        var componentInformation = ReadComponentInformation(stream);

        var id = new ProductComponentIdentity(componentInformation.Id, componentInformation.InformationalVersion);
        var fileName = additionalInformation.OverrideFileName ?? componentInformation.FileName;
        var filePath = _fileSystem.Path.Combine(installLocation, fileName);

        return new SingleFileComponent(id, installLocation, fileName, null)
        {
            Name = componentInformation.Name,
            InstallationSize = GetSize(componentInformation.Size, additionalInformation.Drive),
            DetectConditions = new[]
            {
                new FileCondition(filePath)
                {
                    Version = componentInformation.FileVersion,
                    IntegrityInformation = new ComponentIntegrityInformation(componentInformation.Hash, FileHashType)
                }
            }
        };
    }

    public Task<IProductReference> ProductReferenceFromFileAsync(IFileInfo file)
    {
        Requires.NotNull(file, nameof(file));

        if (!file.Exists)
            throw new FileNotFoundException("Component assembly not found.", file.FullName);

        return Task.Run(() =>
        {
            using var assemblyStream = file.OpenRead();
            return ProductReferenceFromStream(assemblyStream);
        });
    }

    public IProductReference ProductReferenceFromAssembly(Assembly assembly)
    {
        Requires.NotNull(assembly, nameof(assembly));

        var assemblyFile = assembly.Location;
        using var assemblyStream = _fileSystem.FileStream.New(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return ProductReferenceFromStream(assemblyStream);
    }

    public IProductReference ProductReferenceFromStream(Stream assemblyStream)
    {
        var productInfo = ReadProductInformation(assemblyStream);
        ProductBranch? branch = null;
        if (productInfo.Version is not null)
            branch = _branchManager.GetBranchFromVersion(productInfo.Version);
        return new ProductReference(productInfo.ProductName, productInfo.Version, branch);
    }

    private ComponentFileInformation ReadComponentInformation(Stream assemblyStream)
    {
        return null;
    }

    private ProductInformation ReadProductInformation(Stream assemblyStream)
    {
        return null;
    }

    private static InstallationSize GetSize(long size, ExtractorAdditionalInformation.InstallDrive drive)
    {
        return drive == ExtractorAdditionalInformation.InstallDrive.System ? new InstallationSize(size, 0) : new InstallationSize(0, size);
    }
}

internal record ProductInformation 
{
    public required string ProductName { get; init; }

    public SemVersion? Version { get; init; }
}


internal record ComponentFileInformation
{
    public required string Id { get; init; }

    public required string FileName { get; init; }

    public required Version FileVersion { get; init; }

    public required long Size { get; init; }

    public required byte[] Hash { get; init; }

    public string? Name { get; init; }

    public SemVersion? InformationalVersion { get; init; }
}