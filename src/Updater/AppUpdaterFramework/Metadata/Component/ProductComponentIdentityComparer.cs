using System;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public sealed class ProductComponentIdentityComparer(
    bool excludeVersion = false,
    StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    : IEqualityComparer<ProductComponentIdentity>
{
    public static readonly ProductComponentIdentityComparer Default = new();
    public static readonly ProductComponentIdentityComparer VersionIndependent = new(true);

    private readonly StringComparer _comparer = comparisonType switch
    {
        StringComparison.CurrentCulture => StringComparer.CurrentCulture,
        StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
        StringComparison.Ordinal => StringComparer.Ordinal,
        StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
        StringComparison.InvariantCulture => StringComparer.InvariantCulture,
        StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
        _ => throw new ArgumentException("The comparison type is not supported", nameof(comparisonType))
    };

    public bool Equals(ProductComponentIdentity? x, ProductComponentIdentity? y)
    {
        if (x == y)
            return true;
        if (x is null || y is null)
            return false;
        if (!x.Id.Equals(y.Id, comparisonType))
            return false;
        if (!excludeVersion)
        {
            if (x.Version != null)
                return x.Version.Equals(y.Version);
            return y.Version == null;
        }

        return true;
    }

    public int GetHashCode(ProductComponentIdentity? obj)
    {
        if (obj == null)
            return 0;
        var hc = new HashCode();
        hc.Add(obj.Id, _comparer);
        if (!excludeVersion && obj.Version != null)
            hc.Add(obj.Version);
        return hc.ToHashCode();
    }
}