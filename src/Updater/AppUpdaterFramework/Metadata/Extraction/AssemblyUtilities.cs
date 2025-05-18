using Mono.Cecil;
using System;
using System.Linq;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Extraction;

internal static class AssemblyUtilities
{
    internal static CustomAttribute? GetSingleAttributeOfType(this ICustomAttributeProvider attributeProvider, Type type)
    {
        var attributes = attributeProvider.CustomAttributes
            .Where(x => x.AttributeType.FullName.Equals(type.FullName));
        return attributes.SingleOrDefault();
    }

    public static string? GetAttributeCtorString(this CustomAttribute attribute, int ctorParamIndex = 0)
    {
        if (!attribute.HasConstructorArguments)
            return null;
        var argument = attribute.ConstructorArguments[ctorParamIndex];
        if (argument.Type.MetadataType != MetadataType.String)
            return null;

        return argument.Value as string;
    }

    public static string? GetAttributePropertyString(this CustomAttribute attribute, string propertyName)
    {
       if (!attribute.HasConstructorArguments)
            return null;

       var property = attribute.Properties.FirstOrDefault(p => p.Name.Equals(propertyName));
        if (property.Argument.Type.MetadataType != MetadataType.String)
            return null;

        return property.Argument.Value as string;
    }
}