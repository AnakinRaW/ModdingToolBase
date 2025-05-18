using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IReadOnlyFileRepository
{
    IFileInfo? GetComponent(InstallableComponent component);

    IDictionary<InstallableComponent, IFileInfo> GetComponents();
}