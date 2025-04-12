using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.Storage;

internal interface IReadOnlyRepository
{
    IFileInfo? GetComponent(IInstallableComponent component);

    IDictionary<IInstallableComponent, IFileInfo> GetComponents();
}