using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Detection;
using AnakinRaW.AppUpdaterFramework.Metadata.Manifest;
using AnakinRaW.AppUpdaterFramework.Utilities;
using AnakinRaW.CommonUtilities.Hashing;
using Semver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace AnakinRaW.AppUpdaterFramework.Json;

public abstract record AppComponentBase(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("version")] string? Version,
    [property: JsonPropertyName("name")] string? Name
)
{
    public ProductComponentIdentity ToIdentity()
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
    public ComponentGroup ToGroup()
    {
        var items = Items ?? [];
        var identity = ToIdentity();
        return new ComponentGroup(identity.Id, identity.Version, items.Select(i => i.ToIdentity()).ToList())
        {
            Name = Name
        };
    }

    public InstallableComponent ToInstallable()
    {
        if (string.IsNullOrEmpty(InstallPath))
            throw new ManifestException($"Illegal manifest: {nameof(InstallPath)} must not be null or empty.");

        if (string.IsNullOrEmpty(FileName))
            throw new ManifestException($"Illegal manifest: {nameof(FileName)} must not be null.");

        if (OriginInfo is null)
            throw new ManifestException($"Illegal manifest: {nameof(OriginInfo)} must not be null.");

        var installationSize = InstallSize.HasValue
            ? new InstallationSize(InstallSize!.Value.SystemDrive, InstallSize.Value.ProductDrive)
            : default;

        var conditions = DetectConditions is null
            ? []
            : DetectConditions.Select(c => c.ToCondition()).ToList();

        var identity = ToIdentity();
        return new SingleFileComponent(identity.Id, identity.Version, InstallPath!, FileName!, OriginInfo.ToOriginInfo())
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
    public IDetectionCondition ToCondition()
    {
        if (Type != ConditionType.File)
            throw new NotSupportedException($"{Type} currently not supported.");

        if (string.IsNullOrEmpty(FilePath))
            throw new ManifestException($"Illegal manifest: {nameof(FilePath)} must not be null or empty.");

        return new SingleFileDetectCondition(FilePath)
        {
            ProductVersion = ManifestHelpers.CreateNullableSemVersion(ProductVersion),
            Version = ManifestHelpers.CreateNullableVersion(Version),
            IntegrityInformation = ManifestHelpers.FromSha256(Sha256),
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
            throw new ManifestException($"Illegal manifest: {nameof(Url)} must not be null or empty.");

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