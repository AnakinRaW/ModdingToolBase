using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;

namespace AnakinRaW.ApplicationBase.Environment;

public abstract class ApplicationEnvironment
{
    protected readonly IFileSystem FileSystem;

    public abstract string ApplicationName { get; }
    
    [field: AllowNull, MaybeNull]
    public string ApplicationLocalPath => LazyInitializer.EnsureInitialized(ref field, BuildLocalPath)!;

    [field: AllowNull, MaybeNull]
    public IDirectoryInfo ApplicationLocalDirectory =>
        field ??= FileSystem.DirectoryInfo.New(ApplicationLocalPath);

    public ApplicationAssemblyInfo AssemblyInfo { get; }

    protected abstract string ApplicationLocalDirectoryName { get; }

    protected ApplicationEnvironment(Assembly assembly, IFileSystem fileSystem)
    {
        if (assembly == null) 
            throw new ArgumentNullException(nameof(assembly));
        FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        AssemblyInfo = new ApplicationAssemblyInfo(assembly, fileSystem);
    }

    private string BuildLocalPath()
    {
        var appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        return FileSystem.Path.Combine(appDataPath, ApplicationLocalDirectoryName);
    }
}