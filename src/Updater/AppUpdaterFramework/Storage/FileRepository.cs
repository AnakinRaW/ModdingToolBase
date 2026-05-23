using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.CommonUtilities;
using AnakinRaW.CommonUtilities.FileSystem;
using AnakinRaW.CommonUtilities.Hashing;

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
        var file = ResolveComponentFile(component, variables);

        if (file.Exists)
            return file;

        file.Create().Dispose();
        file.Refresh();
        if (!file.Exists)
            throw new InvalidOperationException();
        return file;
    }

    private IFileInfo ResolveComponentFile(InstallableComponent component, IReadOnlyDictionary<string, string> variables)
    {
        var deterministicName = CreateDeterministicFileName(component, variables);
        if (deterministicName is not null)
            return _fileSystem.FileInfo.New(_fileSystem.Path.Combine(Root.FullName, deterministicName));

        var prefix = GetNamePrefix(component, variables);
        for (var i = 0; i < 10; i++)
        {
            var file = _fileSystem.FileInfo.New(CreateRandomFilePath(prefix));
            if (!file.Exists)
                return file;
        }
        throw new InvalidOperationException("Unable to create a new random file after 10 retries");
    }

    private string? CreateDeterministicFileName(InstallableComponent component, IReadOnlyDictionary<string, string> variables)
    {
        var integrity = component.OriginInfo?.IntegrityInformation;
        if (integrity is null || integrity.Value.HashType == HashTypeKey.None || integrity.Value.Hash is null)
            return null;

        var hashHex = ToHex(integrity.Value.Hash);
        var prefix = GetNamePrefix(component, variables);
        prefix = string.IsNullOrEmpty(prefix) ? null : prefix!.TrimEnd('.');

        return string.IsNullOrEmpty(prefix)
            ? $"{hashHex}.{NewFileExtension}"
            : $"{prefix}.{hashHex}.{NewFileExtension}";
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
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