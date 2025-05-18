using System;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

public sealed class ProductReferenceEqualityComparer : IEqualityComparer<ProductReference>
{
    public static readonly ProductReferenceEqualityComparer Default = new(true, true);
    public static readonly ProductReferenceEqualityComparer VersionAware = new(true, false);
    public static readonly ProductReferenceEqualityComparer BranchAware = new(false, true);
    public static readonly ProductReferenceEqualityComparer NameOnly = new(false, false);

    private readonly bool _compareVersion;
    private readonly bool _compareBranch;

    private ProductReferenceEqualityComparer(bool compareVersion, bool compareBranch)
    {
        _compareVersion = compareVersion;
        _compareBranch = compareBranch;
    }

    public bool Equals(ProductReference? x, ProductReference? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;

        if (!x.Name.Equals(y.Name))
            return false;

        if (_compareBranch)
        {
            if (!Equals(x.Branch, y.Branch))
                return false;
        }

        if (_compareVersion)
            return x.Version != null ? x.Version.Equals(y.Version) : y.Version == null;

        return true;
    }

    public int GetHashCode(ProductReference obj)
    {
        return _compareBranch switch
        {
            true when _compareVersion => HashCode.Combine(obj.Name, obj.Branch, obj.Version),
            true => HashCode.Combine(obj.Name, obj.Branch),
            _ => _compareVersion ? HashCode.Combine(obj.Name, obj.Version) : obj.Name.GetHashCode()
        };
    }

}