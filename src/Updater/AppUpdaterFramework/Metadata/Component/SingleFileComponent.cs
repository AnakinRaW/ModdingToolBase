using System;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

/// <summary>
/// A component representing a single file.
/// </summary>
/// <remarks>
/// The property <see cref="IInstallableComponent.DetectConditions"/> shall not get used for installation detection.
/// </remarks>
public class SingleFileComponent : InstallableComponent, IPhysicalInstallable
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

    internal IFileInfo GetFile(IServiceProvider serviceProvider, ProductVariables? variables = null)
    {
        if (_fileInfo is null)
        {
            var path = GetFullPath(serviceProvider, variables);
            var fs = serviceProvider.GetRequiredService<IFileSystem>();
            _fileInfo = fs.FileInfo.New(path);
        }
        return _fileInfo;
    }

    public override string GetFullPath(IServiceProvider serviceProvider, ProductVariables? variables = null)
    {
        if (_fullPath is null)
        {
            var variablesDict = variables?.ToDictionary();
            var variableResolver = serviceProvider.GetRequiredService<IVariableResolver>();
            var fileName = variableResolver.ResolveVariables(FileName, variablesDict);
            var installPath = variableResolver.ResolveVariables(InstallPath, variablesDict);
            var fs = serviceProvider.GetRequiredService<IFileSystem>();
            _fullPath = fs.Path.Combine(installPath, fileName);
        }
        return _fullPath;
    }
}