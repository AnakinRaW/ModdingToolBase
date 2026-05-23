using System;
using System.Collections.Generic;
using System.Linq;
using AnakinRaW.AppUpdaterFramework.Configuration;
using AnakinRaW.AppUpdaterFramework.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Restart;

internal sealed class PendingUpdateService(IServiceProvider serviceProvider) : IPendingUpdateService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly object _lock = new();
    private byte[]? _bytes;
    private string? _branch;
    private readonly List<PendingComponent> _components = [];

    public byte[]? FetchedManifestBytes
    {
        get
        {
            lock (_lock)
                return _bytes;
        }
    }

    public string? FetchedBranch
    {
        get
        {
            lock (_lock)
                return _branch;
        }
    }

    public IReadOnlyCollection<PendingComponent> PendingComponents
    {
        get
        {
            lock (_lock) 
                return _components.ToList();
        }
    }

    public void SetFetchedManifest(byte[] bytes, string? branch)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));
        lock (_lock)
        {
            _bytes = bytes;
            _branch = branch;
        }
    }

    public void AddPendingComponent(PendingComponent component)
    {
        if (component is null)
            throw new ArgumentNullException(nameof(component));
        lock (_lock)
            _components.Add(component);
    }

    public void ReplacePendingComponents(IEnumerable<PendingComponent> components)
    {
        if (components is null)
            throw new ArgumentNullException(nameof(components));
        lock (_lock)
        {
            _components.Clear();
            _components.AddRange(components);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _bytes = null;
            _branch = null;
            _components.Clear();
        }
    }

    public void BackupPendingComponents()
    {
        var config = _serviceProvider.GetRequiredService<IUpdateConfigurationProvider>().GetConfiguration();
        if (config.BackupPolicy == BackupPolicy.Disable)
            return;

        var backupManager = _serviceProvider.GetRequiredService<IBackupManager>();
        foreach (var pending in PendingComponents)
            backupManager.BackupComponent(pending.Component);
    }
}
