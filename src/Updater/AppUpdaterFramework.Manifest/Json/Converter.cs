using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Metadata.Component.Detection;
using AnakinRaW.AppUpdaterFramework.Metadata.Product;

namespace AnakinRaW.AppUpdaterFramework.Json;

public static class Converter
{
    public static AppComponent ToAppComponent(this IProductComponent component)
    {
        if (component is IComponentGroup group)
            return FromGroup(group);
        if (component is SingleFileComponent file)
            return FromFile(file);

        throw new NotSupportedException();
    }

    public static ApplicationManifest ToApplicationManifest(this IProductReference productReference,
        IEnumerable<IProductComponent> components)
    {
        var appComponents = components.Select(ToAppComponent).ToList();

        return new ApplicationManifest(
            productReference.Name,
            productReference.Version?.ToString(),
            productReference.Branch?.Name,
            appComponents);
    }

    private static AppComponent FromGroup(IComponentGroup group)
    {
        return new AppComponent(
            group.Id, 
            group.Version?.ToString(), 
            group.Name, 
            ComponentType.Group,
            group.Components.Select(ToComponentId).ToList(), 
            null,
            null, 
            null, 
            null, 
            null);
    }

    private static ComponentId ToComponentId(IProductComponentIdentity identity)
    {
        return new ComponentId(identity.Id, identity.Version?.ToString());
    }

    private static AppComponent FromFile(SingleFileComponent fileComponent)
    {
        OriginInfo? originInfo = null;
        if (fileComponent.OriginInfo is not null)
        {
            var orgInfo = fileComponent.OriginInfo;
            originInfo = new OriginInfo(orgInfo.Url.AbsoluteUri, orgInfo.Size, ByteArrayToString(orgInfo.IntegrityInformation.Hash));
        }

        return new AppComponent(
            fileComponent.Id,
            fileComponent.Version?.ToString(),
            fileComponent.Name,
            ComponentType.File,
            null,
            originInfo,
            fileComponent.InstallPath,
            fileComponent.FileName,
            new InstallSize(fileComponent.InstallationSize.SystemDrive, fileComponent.InstallationSize.ProductDrive),
            fileComponent.DetectConditions.Select(ToDetectCondition).ToList());
    }

    private static DetectCondition ToDetectCondition(IDetectionCondition condition)
    {
        if (condition is not SingleFileDetectCondition fileCondition)
            throw new NotSupportedException($"Condition {condition.Type} not supported");
        return new DetectCondition(
            ConditionType.File, 
            fileCondition.FilePath, 
            fileCondition.Version?.ToString(),
            fileCondition.ProductVersion?.ToString(),
            ByteArrayToString(fileCondition.IntegrityInformation.Hash));
    }

    public static string? ByteArrayToString(byte[]? array)
    {
        if (array is null)
            return null;
        var hex = new StringBuilder(array.Length * 2);
        foreach (var b in array)
            hex.AppendFormat("{0:x2}", b);
        return hex.ToString();
    }
}