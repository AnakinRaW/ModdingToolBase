using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Product;
using AnakinRaW.CommonUtilities.FileSystem;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal abstract class FileRepository(IServiceProvider serviceProvider) : IFileRepository
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ConcurrentDictionary<IInstallableComponent, IFileInfo> _componentStore = new(ProductComponentIdentityComparer.Default);
    private readonly IProductService _productService = serviceProvider.GetRequiredService<IProductService>();

    protected readonly IFileSystem FileSystem = serviceProvider.GetRequiredService<IFileSystem>();

    protected abstract IDirectoryInfo Root { get; }

    protected virtual string FileExtensions => "new";

    public IFileInfo AddComponent(IInstallableComponent component)
    {
        if (component == null)
            throw new ArgumentNullException(nameof(component));
        return _componentStore.GetOrAdd(component, CreateComponentFile);
    }

    public IFileInfo? GetComponent(IInstallableComponent component)
    {
        if (component == null) 
            throw new ArgumentNullException(nameof(component));
        _componentStore.TryGetValue(component, out var file);
        return file;
    }

    public IDictionary<IInstallableComponent, IFileInfo> GetComponents()
    {
        return new Dictionary<IInstallableComponent, IFileInfo>(_componentStore, ProductComponentIdentityComparer.Default);
    }

    public void RemoveComponent(IInstallableComponent component)
    {
        if (!_componentStore.TryRemove(component, out var file))
            return;
        file.DeleteWithRetry();
    }

    private IFileInfo CreateComponentFile(IInstallableComponent component)
    {
        var namePrefix = GetNamePrefix(component);

        IFileInfo file = null!;
        for (var i = 0; i < 10; i++)
        {
            file = FileSystem.FileInfo.New(CreateRandomFilePath(namePrefix));
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

    private string? GetNamePrefix(IInstallableComponent component)
    {
        if (component is not SingleFileComponent singleFileComponent)
            return null;
        var file = singleFileComponent.GetFile(_serviceProvider, _productService.GetCurrentInstance().Variables);
        return file.Name;
    }
    
    private string CreateRandomFilePath(string? namePrefix)
    {
        var fileName = CreateFileName(namePrefix);
        return FileSystem.Path.Combine(Root.FullName, fileName);
    }

    private string CreateFileName(string? prefix)
    {
        var randomFilePart = FileSystem.Path.GetFileNameWithoutExtension(FileSystem.Path.GetRandomFileName());
        if (string.IsNullOrEmpty(prefix) || string.IsNullOrWhiteSpace(prefix))
            return $"{randomFilePart}.{FileExtensions}";
        prefix = prefix!.TrimEnd('.');
        return $"{prefix}.{randomFilePart}.{FileExtensions}";
    }
}