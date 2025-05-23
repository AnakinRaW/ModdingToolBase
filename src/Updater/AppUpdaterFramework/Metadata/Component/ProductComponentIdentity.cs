﻿using System;
using AnakinRaW.CommonUtilities;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public class ProductComponentIdentity : IEquatable<ProductComponentIdentity>
{
    public string Id { get; }
    
    public SemVersion? Version { get; }

    public ProductComponentIdentity(string id, SemVersion? version = null)
    {
        ThrowHelper.ThrowIfNullOrEmpty(id);
        Id = id;
        Version = version;
    }

    public override string ToString()
    {
        return GetUniqueId(false);
    }

    public string GetUniqueId()
    {
        return GetUniqueId(false);
    }

    public string GetUniqueId(bool excludeVersion)
    {
        return Format(this, excludeVersion);
    }

    public bool Equals(ProductComponentIdentity? other)
    {
        return ProductComponentIdentityComparer.Default.Equals(this, other);
    }

    internal static string Format(ProductComponentIdentity identity, bool excludeVersion = false)
    {
        if (identity == null) 
            throw new ArgumentNullException(nameof(identity));
        return Utilities.FormatIdentity(identity.Id, excludeVersion ? null : identity.Version, null);
    }
}