using System.IO;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

internal interface IAssemblyMetadataExtractor
{
    ComponentFileInformation ReadComponentInformation(Stream assemblyStream);

    ProductInformation ReadProductInformation(Stream assemblyStream);
}