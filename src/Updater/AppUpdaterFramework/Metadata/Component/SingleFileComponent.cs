using AnakinRaW.CommonUtilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Utilities;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

/// <summary>
/// A component representing a single file.
/// </summary>
/// <remarks>
/// The property <see cref="IInstallableComponent.DetectConditions"/> shall not get used for installation detection.
/// </remarks>
public sealed class SingleFileComponent : InstallableComponent, IPhysicalInstallable
{
    public override ComponentType Type => ComponentType.File;

    private IFileInfo? _fileInfo;
    private string? _fullPath;

    /// <inheritdoc/>
    public string InstallPath { get; }

    public string FileName { get; }

    public SingleFileComponent(IProductComponentIdentity identity, string installPath, string fileName, OriginInfo? originInfo) 
        : base(identity, originInfo)
    {
        ThrowHelper.ThrowIfNullOrEmpty(installPath);
        ThrowHelper.ThrowIfNullOrEmpty(fileName);
        InstallPath = installPath;
        FileName = fileName;
    }

    internal IFileInfo GetFile(IServiceProvider serviceProvider, IReadOnlyDictionary<string, string> variables)
    {
        if (_fileInfo is null)
        {
            var path = GetFullPath(serviceProvider, variables);
            var fs = serviceProvider.GetRequiredService<IFileSystem>();
            _fileInfo = fs.FileInfo.New(path);
        }
        return _fileInfo;
    }

    public override string GetFullPath(IServiceProvider serviceProvider, IReadOnlyDictionary<string, string> variables)
    {
        if (_fullPath is null)
        {
            var fileName = StringTemplateEngine.ResolveVariables(FileName, variables);
            var installPath = StringTemplateEngine.ResolveVariables(InstallPath, variables);
            var fs = serviceProvider.GetRequiredService<IFileSystem>();
            _fullPath = fs.Path.Combine(installPath, fileName);
        }
        return _fullPath;
    }
}