using System;

namespace AnakinRaW.AppUpdaterFramework.Attributes;

[AttributeUsage(AttributeTargets.Assembly)]
public class UpdateProductAttribute : Attribute
{
    public string ProductName { get; }

    public UpdateProductAttribute(string productName)
    {
        if (productName == null)
            throw new ArgumentNullException(nameof(productName));
        if (string.IsNullOrEmpty(productName))
            throw new ArgumentException("Id must not be empty.");
        ProductName = productName;
    }
}