using System;
using System.IO;
using System.Reflection;
using AnakinRaW.AppUpdaterFramework.Attributes;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Extraction;

internal class CecilMetadataExtractor(IServiceProvider serviceProvider)
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

    public ExtractedProductInformation ReadProductInformation(Stream assemblyStream)
    {
        assemblyStream.Position = 0;

        using var assembly = GetAssemblyDefinition(assemblyStream);

        var infoVersion = GetInformationalVersion(assembly);
        var productName = GetProductName(assembly);

        return new ExtractedProductInformation
        {
            ProductName = productName,
            Version = infoVersion
        };
    }

    private static string GetComponentId(AssemblyDefinition assemblyDefinition)
    {
        return assemblyDefinition.GetSingleAttributeOfType(typeof(UpdateComponentAttribute))
                   ?.GetAttributeCtorString() ??
               throw new InvalidOperationException(
                   $"The specified assembly does not contain the {nameof(UpdateComponentAttribute)} attribute.");
    }

    private static string? GetComponentName(AssemblyDefinition assemblyDefinition)
    {
        return assemblyDefinition.GetSingleAttributeOfType(typeof(UpdateComponentAttribute))
            ?.GetAttributePropertyString(nameof(UpdateComponentAttribute.Name));
    }

    private static Version? GetFileVersion(AssemblyDefinition assemblyDefinition)
    {
        var fileVersion = assemblyDefinition.GetSingleAttributeOfType(typeof(AssemblyFileVersionAttribute))?.GetAttributeCtorString();
        return fileVersion is null ? null : Version.Parse(fileVersion);
    }

    private static string GetProductName(AssemblyDefinition assemblyDefinition)
    {
        return assemblyDefinition.GetSingleAttributeOfType(typeof(UpdateProductAttribute))?.GetAttributeCtorString() ??
            throw new InvalidOperationException($"The specified assembly does not contain the {nameof(UpdateProductAttribute)} attribute.");
    }

    private static SemVersion? GetInformationalVersion(AssemblyDefinition assemblyDefinition)
    {
        var infoVersion = assemblyDefinition.GetSingleAttributeOfType(typeof(AssemblyInformationalVersionAttribute))?.GetAttributeCtorString();
        return infoVersion is null ? null : SemVersion.Parse(infoVersion, SemVersionStyles.Any);
    }

    private static AssemblyDefinition GetAssemblyDefinition(Stream assemblyStream)
    {
        return AssemblyDefinition.ReadAssembly(assemblyStream, new ReaderParameters { ReadSymbols = false });
    }
}