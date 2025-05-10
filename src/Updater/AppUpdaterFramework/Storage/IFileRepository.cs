using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IFileRepository : IReadOnlyFileRepository
{
    IFileInfo AddComponent(IInstallableComponent component, IReadOnlyDictionary<string, string> variables);

    void RemoveComponent(IInstallableComponent component);
}