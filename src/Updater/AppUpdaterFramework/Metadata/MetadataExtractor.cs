﻿using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Conditions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

internal sealed class MetadataExtractor : IMetadataExtractor
{
    private static readonly HashTypeKey FileHashType = HashTypeKey.SHA256;

    private readonly IFileSystem _fileSystem;
    private readonly IBranchManager _branchManager;
    private readonly IAssemblyMetadataExtractor _assemblyMetadataExtractor;

    public MetadataExtractor(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null) 
            throw new ArgumentNullException(nameof(serviceProvider));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _branchManager = serviceProvider.GetRequiredService<IBranchManager>();
        _assemblyMetadataExtractor = serviceProvider.GetService<IAssemblyMetadataExtractor>() ?? new AssemblyMetadataExtractor(serviceProvider);
    }

    public IInstallableComponent ComponentFromAssembly(Assembly assembly, string installLocation,
        ExtractorAdditionalInformation additionalInformation = default)
    {
        if (assembly == null) 
            throw new ArgumentNullException(nameof(assembly));
        ThrowHelper.ThrowIfNullOrEmpty(installLocation);

        var assemblyFile = assembly.Location;
        using var assemblyStream = _fileSystem.FileStream.New(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return ComponentFromStream(assemblyStream, installLocation, additionalInformation);
    }

    public Task<IInstallableComponent> ComponentFromFileAsync(IFileInfo file, string installLocation,
        ExtractorAdditionalInformation additionalInformation = default)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        ThrowHelper.ThrowIfNullOrEmpty(installLocation);

        if (!file.Exists)
            throw new FileNotFoundException("Component assembly not found.", file.FullName);

        return Task.Run(() =>
        {
            using var assemblyStream = file.OpenRead();
            return ComponentFromStream(assemblyStream, installLocation, additionalInformation);
        });
    }

    public ComponentFileInformation InformationFromStream(Stream stream)
    {
        return _assemblyMetadataExtractor.ReadComponentInformation(stream);
    }

    public IInstallableComponent ComponentFromStream(Stream stream, string installLocation,
        ExtractorAdditionalInformation additionalInformation = default)
    {
        if (stream == null) 
            throw new ArgumentNullException(nameof(stream));
        ThrowHelper.ThrowIfNullOrEmpty(installLocation);

        var componentInformation = InformationFromStream(stream);

        var id = new ProductComponentIdentity(componentInformation.Id, componentInformation.InformationalVersion);
        var fileName = additionalInformation.OverrideFileName ?? componentInformation.FileName;
        var filePath = _fileSystem.Path.Combine(installLocation, fileName);
        var integrityInfo = new ComponentIntegrityInformation(componentInformation.Hash, FileHashType);
        var originInfo = CreateOriginInfo(additionalInformation.Origin, integrityInfo, componentInformation.Size);

        return new SingleFileComponent(id, installLocation, fileName, originInfo)
        {
            Name = componentInformation.Name,
            InstallationSize = GetSize(componentInformation.Size, additionalInformation.Drive),
            DetectConditions =
            [
                new FileCondition(filePath)
                {
                    Version = componentInformation.FileVersion,
                    IntegrityInformation = integrityInfo
                }
            ]
        };
    }

    public Task<IProductReference> ProductReferenceFromFileAsync(IFileInfo file)
    {
        if (file == null) 
            throw new ArgumentNullException(nameof(file));

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
        if (assembly == null) 
            throw new ArgumentNullException(nameof(assembly));

        var assemblyFile = assembly.Location;
        using var assemblyStream = _fileSystem.FileStream.New(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return ProductReferenceFromStream(assemblyStream);
    }

    public IProductReference ProductReferenceFromStream(Stream assemblyStream)
    {
        var productInfo = _assemblyMetadataExtractor.ReadProductInformation(assemblyStream);
        ProductBranch? branch = null;
        if (productInfo.Version is not null)
            branch = _branchManager.GetBranchFromVersion(productInfo.Version);
        return new ProductReference(productInfo.ProductName, productInfo.Version, branch);
    }

    private OriginInfo? CreateOriginInfo(Uri? origin, ComponentIntegrityInformation integrityInformation, long? size = null)
    {
        if (origin is null)
            return null;
        if (!origin.IsAbsoluteUri)
            throw new InvalidOperationException("Origin uri must be absolute");

        return new OriginInfo(origin)
        {
            Size = size,
            IntegrityInformation = integrityInformation
        };
    }

    private static InstallationSize GetSize(long size, ExtractorAdditionalInformation.InstallDrive drive)
    {
        return drive == ExtractorAdditionalInformation.InstallDrive.System ? new InstallationSize(size, 0) : new InstallationSize(0, size);
    }
}