using System;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public interface IPhysicalInstallable : IInstallableComponent
{
    /// <summary>
    /// Directory where this component gets installed to. May contain product variables.
    /// </summary>
    string InstallPath { get; }

    string GetFullPath(IServiceProvider serviceProvider, IReadOnlyDictionary<string, string> variables);
}