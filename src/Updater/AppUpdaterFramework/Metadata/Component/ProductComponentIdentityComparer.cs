using System;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public class ProductComponentIdentityComparer : IEqualityComparer<IProductComponentIdentity>
{
    public static readonly ProductComponentIdentityComparer Default = new();
    public static readonly ProductComponentIdentityComparer VersionIndependent = new(true);
    private readonly StringComparison _comparisonType;
    private readonly bool _excludeVersion;
    private readonly StringComparer _comparer;

    public ProductComponentIdentityComparer(
        bool excludeVersion = false,
        StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        _excludeVersion = excludeVersion;
        _comparisonType = comparisonType;
        _comparer = comparisonType switch
        {
            StringComparison.CurrentCulture => StringComparer.CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
            StringComparison.Ordinal => StringComparer.Ordinal,
            StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
            StringComparison.InvariantCulture => StringComparer.InvariantCulture,
            StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
            _ => throw new ArgumentException("The comparison type is not supported", nameof(comparisonType))
        };
    }

    public bool Equals(IProductComponentIdentity? x, IProductComponentIdentity? y)
    {
        if (x == y)
            return true;
        if (x is null || y is null)
            return false;
        if (!x.Id.Equals(y.Id, _comparisonType))
            return false;
        if (!_excludeVersion)
        {
            if (x.Version != null)
                return x.Version.Equals(y.Version);
            return y.Version == null;
        }

        return true;
    }

    public int GetHashCode(IProductComponentIdentity? obj)
    {
        if (obj == null)
            return 0;
        var num = 0;
        num ^= _comparer.GetHashCode(obj.Id);
        if (!_excludeVersion && obj.Version != null)
            num ^= obj.Version.GetHashCode();
        return num;
    }
}