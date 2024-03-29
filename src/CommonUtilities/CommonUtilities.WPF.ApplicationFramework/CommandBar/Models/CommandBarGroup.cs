using System.Collections.Generic;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.CommandBar.Models;

internal class CommandBarGroup(IReadOnlyList<ICommandBarItemDefinition> items) : ICommandBarGroup
{
    public IReadOnlyList<ICommandBarItemDefinition> Items { get; } = items;
}