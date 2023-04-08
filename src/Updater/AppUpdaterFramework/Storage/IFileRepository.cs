using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

public interface IReadonlyRepository
{
    IFileInfo? GetComponent(IInstallableComponent component);

    IDictionary<IInstallableComponent, IFileInfo> GetComponents();

    ISet<IFileInfo> GetFiles();
}

internal interface IFileRepository : IReadonlyRepository
{
    IFileInfo AddComponent(IInstallableComponent component);

    void RemoveComponent(IInstallableComponent component);

    void Clear();
}