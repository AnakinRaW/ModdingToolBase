﻿using Semver;
using Validation;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public class ProductComponentIdentity : IProductComponentIdentity
{
    public string Id { get; }
    public SemVersion? Version { get; }

    public ProductComponentIdentity(string id, SemVersion? version = null)
    {
        Requires.NotNullOrEmpty(id, nameof(id));
        Id = id;
        Version = version;
    }

    public override string ToString()
    {
        return GetUniqueId(false);
    }

    public string GetUniqueId() => GetUniqueId(false);

    public string GetUniqueId(bool excludeVersion) => Format(this, excludeVersion);

    public bool Equals(IProductComponentIdentity? other)
    {
        return ProductComponentIdentityComparer.Default.Equals(this, other);
    }

    internal static string Format(IProductComponentIdentity identity, bool excludeVersion = false)
    {
        Requires.NotNull(identity, nameof(identity));
        return Utilities.FormatIdentity(identity.Id, excludeVersion ? null : identity.Version, null);
    }
}