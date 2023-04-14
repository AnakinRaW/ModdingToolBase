using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Validation;

namespace AnakinRaW.ApplicationBase;

public abstract class ApplicationEnvironmentBase : IApplicationEnvironment
{
    private string? _launcherLocalPath;
    private IDirectoryInfo? _localDirectory;
    private readonly IFileSystem _fileSystem;

    public abstract string ApplicationName { get; }
    public abstract Uri? RepositoryUrl { get; }
    public abstract ICollection<Uri> UpdateMirrors { get; }
    public abstract string ApplicationRegistryPath { get; }
    public string ApplicationLocalPath => LazyInitializer.EnsureInitialized(ref _launcherLocalPath, BuildLocalPath)!;
    public IDirectoryInfo ApplicationLocalDirectory =>
        _localDirectory ??= _fileSystem.DirectoryInfo.New(ApplicationLocalPath);

    public ApplicationAssemblyInfo AssemblyInfo { get; }

    protected abstract string ApplicationLocalDirectoryName { get; }

    protected ApplicationEnvironmentBase(Assembly assembly, IServiceProvider serviceProvider)
    {
        Requires.NotNull(assembly, nameof(assembly));
        Requires.NotNull(serviceProvider, nameof(serviceProvider));
        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        AssemblyInfo = new ApplicationAssemblyInfo(assembly);
    }

    private string BuildLocalPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return _fileSystem.Path.Combine(appDataPath, ApplicationLocalDirectoryName);
    }
}