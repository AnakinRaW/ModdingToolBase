using System.Collections.Generic;
using Semver;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public sealed class ComponentGroup(
    string id,
    SemVersion? version,
    IReadOnlyCollection<ProductComponentIdentity> components)
    : ProductComponent(id, version)
{
    public override ComponentType Type => ComponentType.Group;

    public IReadOnlyCollection<ProductComponentIdentity> Components { get; } = components;
}