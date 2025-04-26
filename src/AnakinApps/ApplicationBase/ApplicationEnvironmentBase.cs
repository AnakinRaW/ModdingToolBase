using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using AnakinRaW.AppUpdaterFramework.Configuration;

namespace AnakinRaW.ApplicationBase;


public abstract class UpdatableApplicationEnvironment(Assembly assembly, IFileSystem fileSystem)
    : ApplicationEnvironment(assembly, fileSystem), IUpdateConfigurationProvider
{
    public abstract UpdateConfiguration UpdateConfiguration { get; }

    public abstract Uri? RepositoryUrl { get; }

    public abstract ICollection<Uri> UpdateMirrors { get; }

    public abstract string UpdateRegistryPath { get; } 
    
    public UpdateConfiguration GetConfiguration()
    {
        return UpdateConfiguration;
    }
}


public abstract class ApplicationEnvironment
{
    private readonly IFileSystem _fileSystem;

    public abstract string ApplicationName { get; }
    
    [field: AllowNull, MaybeNull]
    public string ApplicationLocalPath => LazyInitializer.EnsureInitialized(ref field, BuildLocalPath)!;

    [field: AllowNull, MaybeNull]
    public IDirectoryInfo ApplicationLocalDirectory =>
        field ??= _fileSystem.DirectoryInfo.New(ApplicationLocalPath);

    public ApplicationAssemblyInfo AssemblyInfo { get; }

    protected abstract string ApplicationLocalDirectoryName { get; }

    protected ApplicationEnvironment(Assembly assembly, IFileSystem fileSystem)
    {
        if (assembly == null) 
            throw new ArgumentNullException(nameof(assembly));
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        AssemblyInfo = new ApplicationAssemblyInfo(assembly);
    }

    private string BuildLocalPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return _fileSystem.Path.Combine(appDataPath, ApplicationLocalDirectoryName);
    }
}