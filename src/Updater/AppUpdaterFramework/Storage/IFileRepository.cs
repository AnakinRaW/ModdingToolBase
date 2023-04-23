using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IFileRepository : IReadonlyRepository
{
    IFileInfo AddComponent(IInstallableComponent component);

    void RemoveComponent(IInstallableComponent component);

    void Clear();
}