using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AnakinRaW.AppUpdaterFramework.Restart;

internal class PendingComponentStore : IWritablePendingComponentStore
{
#if NETSTANDARD2_1
    private readonly ConcurrentBag<PendingComponent> _pendingComponents = new();
#else
    private readonly ConcurrentStack<PendingComponent> _pendingComponents = new();
#endif

    public IReadOnlyCollection<PendingComponent> PendingComponents => _pendingComponents.ToList();

    public PendingComponentStore()
    {
    }

    public void AddComponent(PendingComponent component)
    {
#if NETSTANDARD2_1
        _pendingComponents.Add(component);
#else
        _pendingComponents.Push(component);
#endif
    }

    public void Clear()
    {
        _pendingComponents.Clear();
    }
}