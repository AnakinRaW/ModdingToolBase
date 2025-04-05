﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using AnakinRaW.AppUpdaterFramework.Conditions;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Catalog;
using AnakinRaW.CommonUtilities.Hashing;
using Semver;

namespace AnakinRaW.AppUpdaterFramework;

public abstract record AppComponentBase(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("name")] string? Name
)
{
    public IProductComponentIdentity ToIdentity()
    {
        return !string.IsNullOrEmpty(Version)
            ? new ProductComponentIdentity(Id, ManifestHelpers.CreateNullableSemVersion(Version))
            : new ProductComponentIdentity(Id);
    }
}

public record ApplicationManifest(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("branch")] string? Branch,
    [property: JsonPropertyName("components")] IReadOnlyList<AppComponent> Components
);

public record AppComponent(
    string Id,
    string? Version,
    string? Name,
    [property: JsonPropertyName("type")] ComponentType Type,
    [property: JsonPropertyName("items")] IReadOnlyList<ComponentId>? Items,
    [property: JsonPropertyName("originInfo")] OriginInfo? OriginInfo,
    [property: JsonPropertyName("installPath")] string? InstallPath,
    [property: JsonPropertyName("fileName")] string? FileName,
    [property: JsonPropertyName("installSizes")] InstallSize? InstallSize,
    [property: JsonPropertyName("detectConditions")] IReadOnlyList<DetectCondition>? DetectConditions
) : AppComponentBase(Id, Version, Name)
{
    public IComponentGroup ToGroup()
    {
        var items = Items ?? Array.Empty<ComponentId>();
        return new ComponentGroup(ToIdentity(), items.Select(i => i.ToIdentity()).ToList())
        {
            Name = Name
        };
    }

    public IInstallableComponent ToInstallable()
    {
        if (string.IsNullOrEmpty(InstallPath))
            throw new CatalogException($"Illegal manifest: {nameof(InstallPath)} must not be null or empty.");

        if (string.IsNullOrEmpty(FileName))
            throw new CatalogException($"Illegal manifest: {nameof(FileName)} must not be null.");

        if (OriginInfo is null)
            throw new CatalogException($"Illegal manifest: {nameof(OriginInfo)} must not be null.");

        var installationSize = InstallSize.HasValue
            ? new InstallationSize(InstallSize!.Value.SystemDrive, InstallSize.Value.ProductDrive)
            : default;

        IReadOnlyList<ICondition> conditions;
        if (DetectConditions is null)
            conditions = Array.Empty<ICondition>();
        else
            conditions = DetectConditions.Select(c => c.ToCondition()).ToList();

        return new SingleFileComponent(ToIdentity(), InstallPath!, FileName!, OriginInfo.ToOriginInfo())
        {
            Name = Name,
            InstallationSize = installationSize,
            DetectConditions = conditions
        };
    }
}

public record ComponentId(string Id, string? Version) : AppComponentBase(Id, Version, null);

public record DetectCondition(
    [property: JsonPropertyName("type")] ConditionType Type,
    [property: JsonPropertyName("filePath")] string FilePath,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("productVersion")] string? ProductVersion,
    [property: JsonPropertyName("sha256")] string? Sha256
)
{
    public ICondition ToCondition()
    {
        if (Type != ConditionType.File)
            throw new NotSupportedException($"{Type} currently not supported");

        if (string.IsNullOrEmpty(FilePath))
            throw new CatalogException($"Illegal manifest: {nameof(FilePath)} must not be null or empty.");

        return new FileCondition(FilePath)
        {
            ProductVersion = ManifestHelpers.CreateNullableSemVersion(ProductVersion),
            Version = ManifestHelpers.CreateNullableVersion(Version),
            IntegrityInformation = ManifestHelpers.FromSha256(Sha256),
            Join = ConditionJoin.And
        };
    }
}

public record struct InstallSize(
    [property: JsonPropertyName("systemDrive")] long SystemDrive,
    [property: JsonPropertyName("productDrive")] long ProductDrive
);

public record OriginInfo(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("size")] long? Size,
    [property: JsonPropertyName("sha256")] string? Sha256
)
{
    public Metadata.Component.OriginInfo ToOriginInfo()
    {
        if (string.IsNullOrEmpty(Url))
            throw new CatalogException($"Illegal manifest: {nameof(Url)} must not be null or empty.");

        return new Metadata.Component.OriginInfo(new Uri(Url, UriKind.Absolute))
        {
            IntegrityInformation = ManifestHelpers.FromSha256(Sha256),
            Size = Size
        };
    }
}

internal static class ManifestHelpers
{
    public static ComponentIntegrityInformation FromSha256(string? hashValue)
    {
        return string.IsNullOrEmpty(hashValue)
            ? ComponentIntegrityInformation.None
            : new ComponentIntegrityInformation(HexTools.StringToByteArray(hashValue!), HashTypeKey.SHA256);
    }

    public static SemVersion? CreateNullableSemVersion(string? version)
    {
        return string.IsNullOrEmpty(version) ? null : SemVersion.Parse(version!, SemVersionStyles.Any);
    }

    public static Version? CreateNullableVersion(string? version)
    {
        return string.IsNullOrEmpty(version) ? null : Version.Parse(version);
    }
}