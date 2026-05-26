using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading;
using AnakinRaW.AppUpdaterFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AnakinRaW.AppUpdaterFramework.Restart;

internal sealed class PendingUpdate : IPendingUpdate
{
    private const string PendingManifestFile = "manifest.json";

    private readonly IFileSystem _fileSystem;
    private readonly ILogger? _logger;
    private readonly string _manifestPath;

    private ConcurrentBag<PendingComponent> _components = [];

    public byte[]? FetchedManifestBytes { get; private set; }

    public string? FetchedBranch { get; private set; }

    public IReadOnlyCollection<PendingComponent> PendingComponents => _components.ToArray();

    public PendingUpdate(IServiceProvider serviceProvider)
    {
        if (serviceProvider is null)
            throw new ArgumentNullException(nameof(serviceProvider));

        _fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger(GetType());

        var location = serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration().PendingUpdateLocation;
        _manifestPath = _fileSystem.Path.Combine(location, PendingManifestFile);
    }

    public void SetFetchedManifest(byte[] bytes, string? branch)
    {
        FetchedManifestBytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        FetchedBranch = branch;
    }

    public void AddPendingComponent(PendingComponent component)
    {
        if (component is null)
            throw new ArgumentNullException(nameof(component));
        _components.Add(component);
    }

    public void Clear()
    {
        FetchedManifestBytes = null;
        FetchedBranch = null;
        Interlocked.Exchange(ref _components, []);
        DeletePersistedPendingUpdate();
    }

    public bool PersistForResume()
    {
        var bytes = FetchedManifestBytes;
        if (bytes is null)
            return false;

        try
        {
            var dir = _fileSystem.Path.GetDirectoryName(_manifestPath);
            if (!string.IsNullOrEmpty(dir))
                _fileSystem.Directory.CreateDirectory(dir!);
            _logger?.LogDebug("Persisting pending manifest to '{Path}'", _manifestPath);
            _fileSystem.File.WriteAllBytes(_manifestPath, bytes);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to persist pending manifest at '{Path}': {Message}", _manifestPath, ex.Message);
            return false;
        }
    }

    public bool TryRestoreManifestFromDisk()
    {
        if (!_fileSystem.File.Exists(_manifestPath))
            return false;

        try
        {
            _logger?.LogDebug("Reading pending manifest from '{Path}'", _manifestPath);
            FetchedManifestBytes = _fileSystem.File.ReadAllBytes(_manifestPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to read pending manifest at '{Path}': {Message}", _manifestPath, ex.Message);
            return false;
        }
    }

    private void DeletePersistedPendingUpdate()
    {
        var dir = _fileSystem.Path.GetDirectoryName(_manifestPath);
        if (string.IsNullOrEmpty(dir) || !_fileSystem.Directory.Exists(dir))
            return;

        try
        {
            _logger?.LogDebug("Deleting pending-update directory '{Path}'", dir);
            _fileSystem.Directory.Delete(dir!, recursive: true);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to delete pending-update directory '{Path}': {Message}", dir, ex.Message);
        }
    }
}
