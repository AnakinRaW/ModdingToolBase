﻿using Semver;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

public class ProductReference : IProductReference
{
    public string Name { get; }
    public SemVersion? Version { get; }
    public ProductBranch? Branch { get; }

    public ProductReference(string name, SemVersion? version = null, ProductBranch? branch = null)
    {
        Requires.NotNullOrEmpty(name, nameof(name));
        Name = name;
        Version = version;
        Branch = branch;
    }

    public override string ToString()
    {
        return $"Product {Name};v:{Version};branch:{Branch}";
    }
}