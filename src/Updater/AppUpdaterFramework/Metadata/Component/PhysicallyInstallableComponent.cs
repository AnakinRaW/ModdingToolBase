using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.CommonUtilities;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public abstract class PhysicallyInstallableComponent : InstallableComponent
{
    /// <summary>
    /// Directory where this component gets installed to. May contain product variables.
    /// </summary>
    public string InstallPath { get; }

    protected PhysicallyInstallableComponent(string id, SemVersion? version, string installPath, OriginInfo? originInfo) 
        : base(id, version, originInfo)
    {
        ThrowHelper.ThrowIfNullOrEmpty(installPath);
        InstallPath = installPath;
    }

    public abstract string GetFullPath(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables);
}