using System.Collections.Generic;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.CommandBar.Models;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.CommandBar.Builder;

internal class MenuModelBuilder(string text, bool enabled = true, string? tooltip = null)
    : CommandBarModelBuilder<IMenuDefinition>
{
    protected override IMenuDefinition BuildCore(IReadOnlyList<ICommandBarGroup> groups)
    {
        return new MenuDefinition(text, enabled, tooltip, groups);
    }
}