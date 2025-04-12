using System.Collections.Generic;
using AnakinRaW.AppUpdaterFramework.Metadata.Component;
using AnakinRaW.AppUpdaterFramework.Updater.Tasks;

namespace AnakinRaW.AppUpdaterFramework.Updater.Progress;

internal class ComponentStepComparer : IEqualityComparer<IComponentStep>
{
    internal static readonly IEqualityComparer<IComponentStep> Default = new ComponentStepComparer();
    
    private static readonly IEqualityComparer<IProductComponentIdentity> Comparer = ProductComponentIdentityComparer.Default;

    private ComponentStepComparer()
    {
    }

    public bool Equals(IComponentStep? x, IComponentStep? y)
    {
#pragma warning disable CS8604 // Possible null reference argument.
        return Comparer.Equals(x?.Component, y?.Component);
#pragma warning restore CS8604 // Possible null reference argument.
    }

    public int GetHashCode(IComponentStep obj)
    {
        return Comparer.GetHashCode(obj.Component);
    }
}