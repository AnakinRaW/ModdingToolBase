using System;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.ApplicationBase.Update;

internal class ApplicationProductService : ProductServiceBase
{
    private readonly IApplicationEnvironment _applicationEnvironment;
    private readonly IMetadataExtractor _metadataExtractor;

    private IDirectoryInfo? _installLocation;

    public override IDirectoryInfo InstallLocation => _installLocation ??= GetInstallLocation();


    public ApplicationProductService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _applicationEnvironment = serviceProvider.GetRequiredService<IApplicationEnvironment>();
        _metadataExtractor = serviceProvider.GetRequiredService<IMetadataExtractor>();
    }

    protected override IProductReference CreateCurrentProductReference()
    {
        return _metadataExtractor.ProductReferenceFromAssembly(_applicationEnvironment.AssemblyInfo.Assembly);
    }

    protected override void AddAdditionalProductVariables(ProductVariables variables, IProductReference product)
    {
        variables.Add(ApplicationVariablesKeys.AppData, _applicationEnvironment.ApplicationLocalPath);
        variables.Add(ApplicationVariablesKeys.AppFileName, _applicationEnvironment.AssemblyInfo.ExecutableFileName);
    }


    private IDirectoryInfo GetInstallLocation()
    {
        var fs = ServiceProvider.GetRequiredService<IFileSystem>();
        var locationPath = _applicationEnvironment.AssemblyInfo.Assembly.Location;
        var directory = fs.FileInfo.New(locationPath).Directory;
        if (directory is null || !directory.Exists)
            throw new DirectoryNotFoundException("Unable to find location of current assembly.");
        return directory;
    }
}