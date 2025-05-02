using System;
using System.Collections.Generic;
using AnakinRaW.CommonUtilities;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

public sealed class ProductBranch : IEquatable<ProductBranch>
{
    public static readonly IEqualityComparer<string> BranchNamEqualityComparer = StringComparer.OrdinalIgnoreCase;

    public string Name { get; }

    public ICollection<Uri> ManifestLocations { get; }

    public bool IsDefault { get; }

    public ProductBranch(string name, bool isDefault) : this(name, [], isDefault)
    {
    }

    public ProductBranch(string name, ICollection<Uri> manifestLocations, bool isDefault)
    {
        ThrowHelper.ThrowIfNullOrEmpty(name);
        Name = name;
        ManifestLocations = manifestLocations ?? throw new ArgumentNullException(nameof(manifestLocations));
        IsDefault = isDefault;
        ValidateManifestUris();
    }

    public override string ToString()
    {
        return Name;
    }

    public bool Equals(ProductBranch? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return BranchNamEqualityComparer.Equals(Name, other.Name);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) 
            return false;
        if (ReferenceEquals(this, obj)) 
            return true;
        return obj is ProductBranch other && Equals(other);
    }

    public override int GetHashCode()
    {
        return BranchNamEqualityComparer.GetHashCode(Name);
    }

    private void ValidateManifestUris()
    {
        foreach (var manifestLocation in ManifestLocations)
        {
            if (manifestLocation is null)
                throw new InvalidOperationException("The branch's manifest location is null.");
            if (!manifestLocation.IsAbsoluteUri)
                throw new InvalidOperationException($"The branch's manifest location: '{manifestLocation.AbsoluteUri}' needs to be an absolute uri.");
        }
    }
}