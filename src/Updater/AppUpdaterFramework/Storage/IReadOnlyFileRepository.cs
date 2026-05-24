using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IReadOnlyFileRepository
{
    string? GetComponent(InstallableComponent component);

    IDictionary<InstallableComponent, string> GetComponents();
}