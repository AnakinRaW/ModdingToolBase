using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading.Tasks;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Metadata;

public interface IMetadataExtractor
{
    Task<IInstallableComponent> ComponentFromFile(IFileInfo file, string installLocation);
    Task<IInstallableComponent> ComponentFromStream(Stream stream, string installLocation);
    Task<IInstallableComponent> ComponentFromAssembly(Assembly file, string installLocation);

    Task<IProductReference> ProductReferenceFromFile(string optionsApplicationFile);
}