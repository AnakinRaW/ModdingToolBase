using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IFileRepository : IReadOnlyFileRepository
{
    string AddComponent(InstallableComponent component, IReadOnlyDictionary<string, string> variables);

    void RemoveComponent(InstallableComponent component);
}