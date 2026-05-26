using System.Collections.Generic;
using System.IO;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;

namespace AnakinRaW.AppUpdaterFramework.External;

internal interface IExternalUpdaterIntegrityCheck
{
    // Reads the bytes from a Stream, hashes the content and then compares against trusted integrity information.
    void EnsureMatchesAny(Stream openHandle, IReadOnlyCollection<ComponentIntegrityInformation> integrityInformation);
}
