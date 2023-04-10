using System;

namespace AnakinRaW.AppUpdaterFramework.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
public class UpdateComponentAttribute : Attribute
{
    public string Id { get; }

    public string? Name { get; set; }

    public UpdateComponentAttribute(string id)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("Id must not be empty.");
        Id = id;
    }
}