using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.External;

internal interface IExternalUpdaterIntegrityCheck
{
    void EnsureMatchesAny(IFileInfo updater, IReadOnlyCollection<ComponentIntegrityInformation> acceptable);
}
