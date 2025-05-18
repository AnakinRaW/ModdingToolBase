using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IFileRepository : IReadOnlyFileRepository
{
    IFileInfo AddComponent(InstallableComponent component, IReadOnlyDictionary<string, string> variables);

    void RemoveComponent(InstallableComponent component);
}