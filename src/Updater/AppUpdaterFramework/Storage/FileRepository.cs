using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.FileSystem;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal sealed class FileRepository : IFileRepository
{
    private readonly ConcurrentDictionary<InstallableComponent, IFileInfo> _componentStore = new(ProductComponentIdentityComparer.Default);
    private readonly IFileSystem _fileSystem;

    private string NewFileExtension { get; }
    private IDirectoryInfo Root { get; }
    
    public FileRepository(string location, IFileSystem fileSystem, string newFileExtension = "new")
    {
        ThrowHelper.ThrowIfNullOrEmpty(location);
        ThrowHelper.ThrowIfNullOrEmpty(newFileExtension);

        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        
        NewFileExtension = newFileExtension;
       
        Root = _fileSystem.DirectoryInfo.New(location);
        Root.Create();
    }

    public IFileInfo AddComponent(InstallableComponent component, IReadOnlyDictionary<string, string> variables)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        return _componentStore.GetOrAdd(component, c => CreateComponentFile(c, variables));
    }

    public IFileInfo? GetComponent(InstallableComponent component)
    {
        if (component == null) 
            throw new ArgumentNullException(nameof(component));
        _componentStore.TryGetValue(component, out var file);
        return file;
    }

    public IDictionary<InstallableComponent, IFileInfo> GetComponents()
    {
        return new Dictionary<InstallableComponent, IFileInfo>(_componentStore, ProductComponentIdentityComparer.Default);
    }

    public void RemoveComponent(InstallableComponent component)
    {
        if (!_componentStore.TryRemove(component, out var file))
            return;
        file.DeleteWithRetry();
    }

    private IFileInfo CreateComponentFile(InstallableComponent component, IReadOnlyDictionary<string, string> variables)
    {
        var namePrefix = GetNamePrefix(component, variables);

        IFileInfo file = null!;
        for (var i = 0; i < 10; i++)
        {
            file = _fileSystem.FileInfo.New(CreateRandomFilePath(namePrefix));
            if (!file.Exists)
                break;
        }

        if (file is null || file.Exists)
            throw new InvalidOperationException("Unable to create a new random file after 10 retries");

        file.Create().Dispose();
        file.Refresh();
        if (!file.Exists)
            throw new InvalidOperationException();
        return file;
    }

    private string? GetNamePrefix(InstallableComponent component, IReadOnlyDictionary<string, string> variables)
    {
        if (component is not SingleFileComponent singleFileComponent)
            return null;
        var file = singleFileComponent.GetFile(_fileSystem, variables);
        return file.Name;
    }
    
    private string CreateRandomFilePath(string? namePrefix)
    {
        var fileName = CreateFileName(namePrefix);
        return _fileSystem.Path.Combine(Root.FullName, fileName);
    }

    private string CreateFileName(string? prefix)
    {
        var randomFilePart = _fileSystem.Path.GetFileNameWithoutExtension(_fileSystem.Path.GetRandomFileName());
        if (string.IsNullOrEmpty(prefix) || string.IsNullOrWhiteSpace(prefix))
            return $"{randomFilePart}.{NewFileExtension}";
        prefix = prefix!.TrimEnd('.');
        return $"{prefix}.{randomFilePart}.{NewFileExtension}";
    }
}