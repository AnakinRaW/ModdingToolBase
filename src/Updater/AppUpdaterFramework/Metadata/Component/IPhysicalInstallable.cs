using System.Collections.Generic;
using System.IO.Abstractions;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public interface IPhysicalInstallable : IInstallableComponent
{
    /// <summary>
    /// Directory where this component gets installed to. May contain product variables.
    /// </summary>
    string InstallPath { get; }

    string GetFullPath(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables);
}