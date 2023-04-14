using System;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Product;

public class ProductBranch : IEquatable<ProductBranch>
{
    public string Name { get; }

    public ICollection<Uri> ManifestLocations { get; }

    public bool IsPrerelease { get; }

    public ProductBranch(string name, ICollection<Uri> manifestLocations, bool isPrerelease)
    {
        Name = name;
        ManifestLocations = manifestLocations;
        IsPrerelease = isPrerelease;
        ValidateManifestUris();
    }

    public override string ToString()
    {
        return Name;
    }

    public bool Equals(ProductBranch? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ProductBranch)obj);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
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