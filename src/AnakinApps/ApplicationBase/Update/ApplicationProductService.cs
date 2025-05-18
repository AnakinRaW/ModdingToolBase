using AnakinRaW.AppUpdaterFramework.Metadata;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Product;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using AnakinRaW.ApplicationBase.Environment;

namespace AnakinRaW.ApplicationBase.Update;

public abstract class ApplicationProductService(ApplicationEnvironment applicationEnvironment, IServiceProvider serviceProvider)
    : ProductServiceBase(serviceProvider)
{
    private readonly AssemblyMetadataExtractor _metadataExtractor = new(serviceProvider);

    protected readonly ApplicationEnvironment ApplicationEnvironment = applicationEnvironment ?? throw new ArgumentNullException(nameof(applicationEnvironment));
    protected readonly IFileSystem FileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    [field: AllowNull, MaybeNull]
    public sealed override IDirectoryInfo InstallLocation => field ?? GetInstallLocation();
    
    protected sealed override ProductReference CreateCurrentProductReference()
    {
        return _metadataExtractor.ProductReferenceFromAssembly(ApplicationEnvironment.AssemblyInfo.Assembly);
    }

    protected override void AddAdditionalProductVariables(IDictionary<string, string> variables, ProductReference product)
    {
        variables.Add(ApplicationVariablesKeys.AppData, ApplicationEnvironment.ApplicationLocalPath);
        variables.Add(ApplicationVariablesKeys.AppFileName, ApplicationEnvironment.AssemblyInfo.ExecutableFileName);
    }

    private IDirectoryInfo GetInstallLocation()
    {
        var fs = ServiceProvider.GetRequiredService<IFileSystem>();
        var locationPath = ApplicationEnvironment.AssemblyInfo.Assembly.Location;
        var directory = fs.FileInfo.New(locationPath).Directory;
        if (directory is null || !directory.Exists)
            throw new DirectoryNotFoundException("Unable to find location of current assembly.");
        return directory;
    }
}