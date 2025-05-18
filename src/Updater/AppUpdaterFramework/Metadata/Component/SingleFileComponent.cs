using AnakinRaW.CommonUtilities;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Utilities;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

/// <summary>
/// A component representing a single file.
/// </summary>
/// <remarks>
/// The property <see cref="InstallableComponent.DetectConditions"/> shall not get used for installation detection.
/// </remarks>
public sealed class SingleFileComponent : PhysicallyInstallableComponent
{
    public override ComponentType Type => ComponentType.File;

    private IFileInfo? _fileInfo;
    private string? _fullPath;

    public string FileName { get; }

    public SingleFileComponent(string id, SemVersion? version, string installPath, string fileName, OriginInfo? originInfo) 
        : base(id, version, installPath, originInfo)
    {
        ThrowHelper.ThrowIfNullOrEmpty(installPath);
        ThrowHelper.ThrowIfNullOrEmpty(fileName);
        FileName = fileName;
    }

    internal IFileInfo GetFile(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables)
    {
        if (_fileInfo is null)
        {
            var path = GetFullPath(fileSystem, variables);
            _fileInfo = fileSystem.FileInfo.New(path);
        }
        return _fileInfo;
    }

    public override string GetFullPath(IFileSystem fileSystem, IReadOnlyDictionary<string, string> variables)
    {
        if (_fullPath is null)
        {
            var fileName = StringTemplateEngine.ResolveVariables(FileName, variables);
            var installPath = StringTemplateEngine.ResolveVariables(InstallPath, variables);
            _fullPath = fileSystem.Path.Combine(installPath, fileName);
        }
        return _fullPath;
    }
}