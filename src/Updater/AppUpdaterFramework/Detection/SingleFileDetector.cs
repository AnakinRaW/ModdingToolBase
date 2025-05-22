using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Detection;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Detection;

internal sealed class SingleFileDetector(IServiceProvider services) : IDetector
{
    private readonly IFileSystem _fileSystem = services.GetRequiredService<IFileSystem>();

    public bool Detect(IDetectionCondition condition, IReadOnlyDictionary<string, string> variables)
    {
        if (condition == null) 
            throw new ArgumentNullException(nameof(condition));
        if (variables == null) 
            throw new ArgumentNullException(nameof(variables));
        if (condition is not SingleFileDetectCondition fileCondition)
            throw new ArgumentException("condition is not FileCondition", nameof(condition));

        var filePath = fileCondition.FilePath;
        filePath = StringTemplateEngine.ResolveVariables(filePath, variables);
        if (string.IsNullOrEmpty(filePath) || !_fileSystem.File.Exists(filePath))
            return false;
        if (fileCondition.IntegrityInformation.HashType != HashTypeKey.None)
        {
            var hashingService = services.GetRequiredService<IHashingService>();
            if (!EvaluateFileHash(hashingService, _fileSystem.FileInfo.New(filePath),
                    fileCondition.IntegrityInformation.HashType, fileCondition.IntegrityInformation.Hash))
                return false;
        }

        var versionInfo = GetFileVersionInfo(filePath);

        if (fileCondition.ProductVersion is not null &&
            !EvaluateProductVersion(versionInfo, fileCondition.ProductVersion))
            return false;

        return fileCondition.Version == null || EvaluateFileVersion(versionInfo, fileCondition.Version);
    }

    private static bool EvaluateFileHash(IHashingService hashingService, IFileInfo file, HashTypeKey hashType, byte[]? expectedHash)
    {
        if (expectedHash is null)
            return false;
        var actualHash = hashingService.GetHash(file, hashType);
        return actualHash.SequenceEqual(expectedHash);
    }

    private IFileVersionInfo? GetFileVersionInfo(string filePath)
    {
        try
        { 
            return _fileSystem.FileVersionInfo.GetVersionInfo(filePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool EvaluateProductVersion(IFileVersionInfo? versionInfo, SemVersion version)
    {
        if (versionInfo is null)
            return false;
        return SemVersion.TryParse(versionInfo.ProductVersion, SemVersionStyles.Any, out var actualVersion) &&
               actualVersion.Equals(version);
    }

    private static bool EvaluateFileVersion(IFileVersionInfo? versionInfo, Version version)
    {
        if (versionInfo is null)
            return false;
        // ReSharper disable once AssignNullToNotNullAttribute
        return Version.TryParse(versionInfo.FileVersion, out var actualVersion) && actualVersion.Equals(version);
    }
}