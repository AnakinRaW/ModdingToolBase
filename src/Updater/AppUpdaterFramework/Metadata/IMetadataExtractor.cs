using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

public interface IMetadataExtractor
{
    IInstallableComponent ComponentFromStream(Stream stream, string installLocation, ExtractorAdditionalInformation additionalInformation = default);

    IInstallableComponent ComponentFromAssembly(Assembly assembly, string installLocation, ExtractorAdditionalInformation additionalInformation = default);

    Task<IInstallableComponent> ComponentFromFileAsync(IFileInfo file, string installLocation, ExtractorAdditionalInformation additionalInformation = default);

    Task<IProductReference> ProductReferenceFromFileAsync(IFileInfo file);

    IProductReference ProductReferenceFromAssembly(Assembly assembly);
}