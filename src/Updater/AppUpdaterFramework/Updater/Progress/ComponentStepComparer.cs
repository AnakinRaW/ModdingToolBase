using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal class ComponentStepComparer : IEqualityComparer<IComponentStep>
{
    internal static readonly IEqualityComparer<IComponentStep> Default = new ComponentStepComparer();
    private readonly IEqualityComparer<IProductComponentIdentity> _comparer;

    private ComponentStepComparer(IEqualityComparer<IProductComponentIdentity>? comparer = null)
    {
        _comparer = comparer ?? ProductComponentIdentityComparer.Default;
    }

    public bool Equals(IComponentStep? x, IComponentStep? y)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        return _comparer.Equals(x?.Component, y?.Component);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    public int GetHashCode(IComponentStep obj)
    {
        return _comparer.GetHashCode(obj.Component);
    }
}