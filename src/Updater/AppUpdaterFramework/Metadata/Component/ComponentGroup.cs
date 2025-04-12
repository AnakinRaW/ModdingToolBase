using System;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public sealed class ComponentGroup : ProductComponent, IComponentGroup
{
    public override ComponentType Type => ComponentType.Group;

    public IReadOnlyList<IProductComponentIdentity> Components { get; }

    public ComponentGroup(IProductComponentIdentity identity, IReadOnlyList<IProductComponentIdentity> components) : base(identity)
    {
        if (identity == null)
            throw new ArgumentNullException(nameof(identity));
        Components = components;
    }
}