using System;
using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public sealed class ComponentGroup : ProductComponent, IComponentGroup
{
    public override ComponentType Type => ComponentType.Group;

    public IReadOnlyCollection<IProductComponentIdentity> Components { get; }

    public ComponentGroup(IProductComponentIdentity identity, IReadOnlyCollection<IProductComponentIdentity> components) : base(identity)
    {
        if (identity == null)
            throw new ArgumentNullException(nameof(identity));
        Components = components;
    }
}