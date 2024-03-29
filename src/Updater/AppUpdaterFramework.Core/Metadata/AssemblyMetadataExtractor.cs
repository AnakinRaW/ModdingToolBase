using System;
using System.IO;
using System.Reflection;
using AnakinRaW.AppUpdaterFramework.Attributes;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using Semver;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

internal class AssemblyMetadataExtractor(IServiceProvider serviceProvider) : IAssemblyMetadataExtractor
{
    private readonly IHashingService _hashingService = serviceProvider.GetRequiredService<IHashingService>();

    public ComponentFileInformation ReadComponentInformation(Stream assemblyStream)
    {
        assemblyStream.Seek(0, SeekOrigin.Begin);
        using var assembly = GetAssemblyDefinition(assemblyStream);
       
        var componentId = GetComponentId(assembly);
        var name = GetComponentName(assembly);
        var fileName = assembly.MainModule.Name;
        var fileVersion = GetFileVersion(assembly);

        assemblyStream.Seek(0, SeekOrigin.Begin);
        var hash = _hashingService.GetHash(assemblyStream, HashTypeKey.SHA256);

        var infoVersion = GetInformationalVersion(assembly);

        return new ComponentFileInformation
        {
            FileName = fileName,
            FileVersion = fileVersion,
            Hash = hash,
            Id = componentId,
            Size = assemblyStream.Length,
            InformationalVersion = infoVersion,
            Name = name
        };
    }

    public ProductInformation ReadProductInformation(Stream assemblyStream)
    {
        assemblyStream.Position = 0;

        using var assembly = GetAssemblyDefinition(assemblyStream);

        var infoVersion = GetInformationalVersion(assembly);
        var productName = GetProductName(assembly);

        return new ProductInformation
        {
            ProductName = productName,
            Version = infoVersion
        };
    }

    private static string GetComponentId(ICustomAttributeProvider assemblyDefinition)
    {
        return assemblyDefinition.CustomAttributes.GetAttributeCtorString(typeof(UpdateComponentAttribute)) ??
               throw new InvalidOperationException($"The specified assembly does not contain the {nameof(UpdateComponentAttribute)} attribute.");
    }

    private static string? GetComponentName(ICustomAttributeProvider assemblyDefinition)
    {
        return assemblyDefinition.CustomAttributes.GetAttributePropertyString(typeof(UpdateComponentAttribute),
            nameof(UpdateComponentAttribute.Name));
    }

    private static Version? GetFileVersion(ICustomAttributeProvider assemblyDefinition)
    {
        var fileVersion = assemblyDefinition.CustomAttributes.GetAttributeCtorString(typeof(AssemblyFileVersionAttribute));
        return fileVersion is null ? null : Version.Parse(fileVersion);
    }

    private static string GetProductName(ICustomAttributeProvider assemblyDefinition)
    {
        return assemblyDefinition.CustomAttributes.GetAttributeCtorString(typeof(UpdateProductAttribute)) ??
               throw new InvalidOperationException($"The specified assembly does not contain the {nameof(UpdateProductAttribute)} attribute.");
    }

    private static SemVersion? GetInformationalVersion(ICustomAttributeProvider assemblyDefinition)
    {
        var infoVersion = assemblyDefinition.CustomAttributes.GetAttributeCtorString(typeof(AssemblyInformationalVersionAttribute));
        return infoVersion is null ? null : SemVersion.Parse(infoVersion, SemVersionStyles.Any);
    }

    private AssemblyDefinition GetAssemblyDefinition(Stream assemblyStream)
    {
        return AssemblyDefinition.ReadAssembly(assemblyStream, new ReaderParameters { ReadSymbols = false });
    }
}