using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.Hashing;
using Microsoft.Extensions.DependencyInjection;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Detection;

internal sealed class SingleFileDetector(IServiceProvider services) : IDetector
{
    public bool Detect(IDetectionCondition condition, IReadOnlyDictionary<string, string> variables)
    {
        if (condition == null) 
            throw new ArgumentNullException(nameof(condition));
        if (variables == null) 
            throw new ArgumentNullException(nameof(variables));

        if (condition is not SingleFileDetectCondition fileCondition)
            throw new ArgumentException("condition is not FileCondition", nameof(condition));

        var fileSystem = services.GetRequiredService<IFileSystem>();


        var filePath = fileCondition.FilePath;
        filePath = StringTemplateEngine.ResolveVariables(filePath, variables);
        if (string.IsNullOrEmpty(filePath) || !fileSystem.File.Exists(filePath))
            return false;
        if (fileCondition.IntegrityInformation.HashType != HashTypeKey.None)
        {
            var hashingService = services.GetRequiredService<IHashingService>();
            if (!EvaluateFileHash(hashingService, fileSystem.FileInfo.New(filePath),
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

    private FileVersionInfo? GetFileVersionInfo(string filePath)
    {
        try
        { 
            return FileVersionInfo.GetVersionInfo(filePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool EvaluateProductVersion(FileVersionInfo? versionInfo, SemVersion version)
    {
        if (versionInfo is null)
            return false;
        return SemVersion.TryParse(versionInfo.ProductVersion, SemVersionStyles.Any, out var actualVersion) &&
               actualVersion.Equals(version);
    }

    private static bool EvaluateFileVersion(FileVersionInfo? versionInfo, Version version)
    {
        if (versionInfo is null)
            return false;
        return Version.TryParse(versionInfo.FileVersion, out var actualVersion) && actualVersion.Equals(version);
    }
}