using System;
using System.IO;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

internal class AssemblyMetadataExtractor : IAssemblyMetadataExtractor
{
    private readonly IHashingService _hashingService;

    public AssemblyMetadataExtractor(IServiceProvider serviceProvider)
    {
        _hashingService = serviceProvider.GetRequiredService<IHashingService>();
    }

    public ComponentFileInformation ReadComponentInformation(Stream assemblyStream)
    {
        assemblyStream.Position = 0;
        return null;
    }

    public ProductInformation ReadProductInformation(Stream assemblyStream)
    {
        assemblyStream.Position = 0;
        return null;
    }
}