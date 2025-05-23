﻿using System;
using System.Text;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

internal static class Utilities
{
    public static string FormatIdentity(string id, object? version, string? branch)
    {
        if (id == null) 
            throw new ArgumentNullException(nameof(id));
        var stringBuilder = new StringBuilder(id);
        if (version != null)
        {
            stringBuilder.Append(",version=");
            stringBuilder.Append(version);
        }
        if (!string.IsNullOrEmpty(branch))
        {
            stringBuilder.Append(",branch=");
            stringBuilder.Append(branch);
        }
        return stringBuilder.ToString();
    }

    public static string GetDisplayName(this ProductComponent component)
    {
        return component.Name ?? component.Id;
    }
}