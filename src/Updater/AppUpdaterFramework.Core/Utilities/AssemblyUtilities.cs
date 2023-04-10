using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace AnakinRaW.AppUpdaterFramework.Utilities;

internal static class AssemblyUtilities
{
    public static string? GetAttributeCtorString(this IEnumerable<CustomAttribute> attributes, Type type, int ctorParamIndex = 0)
    {
        var attribute = attributes.FirstOrDefault(x => x.AttributeType.FullName.Equals(type.FullName));
        if (attribute is null || !attribute.HasConstructorArguments)
            return null;

        var argument = attribute.ConstructorArguments[ctorParamIndex];
        if (argument.Type.MetadataType != MetadataType.String)
            return null;

        return argument.Value as string;
    }

    public static string? GetAttributePropertyString(this IEnumerable<CustomAttribute> attributes, Type type, string propertyName)
    {
        var attribute = attributes.FirstOrDefault(x => x.AttributeType.FullName.Equals(type.FullName));
        if (attribute is null || !attribute.HasConstructorArguments)
            return null;

        var property = attribute.Properties.FirstOrDefault(p => p.Name.Equals(propertyName));
        if (property.Argument.Type.MetadataType != MetadataType.String)
            return null;

        return property.Argument.Value as string;
    }


}