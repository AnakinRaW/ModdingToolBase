using System.Collections.Generic;

namespace AnakinRaW.AppUpdaterFramework.Metadata.Component;

public interface IComponentGroup : IProductComponent
{
    IReadOnlyCollection<IProductComponentIdentity> Components { get; } 
}