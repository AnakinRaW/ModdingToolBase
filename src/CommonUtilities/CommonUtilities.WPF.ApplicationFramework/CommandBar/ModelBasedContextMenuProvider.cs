﻿using System.Windows.Controls;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.CommandBar.Models;
using AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Controls;
using AnakinRaW.CommonUtilities.Wpf.Input;
using AnakinRaW.CommonUtilities.Wpf.Utilities;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.CommandBar;

public sealed class ModelBasedContextMenuProvider : CommandBarProvider<ContextMenuDefinition, StylingContextMenu>, 
    IContextMenuProvider<ContextMenuDefinition>
{
    private readonly WeakValueDictionary<ContextMenuDefinition, ContextMenu> _cache = new();

    protected override StylingContextMenu CreateControl()
    {
        return new StylingContextMenu();
    }

    public new ContextMenu Provide(ContextMenuDefinition data)
    {
        if (_cache.TryGetValue(data, out var contextMenu) && contextMenu is not null)
            return contextMenu;
        contextMenu = base.Provide(data);
        _cache[data] = contextMenu;
        return contextMenu;
    }

    public ContextMenu? Provide(object data)
    {
        if (data is ContextMenuDefinition ctxMenuDefinition)
            return Provide(ctxMenuDefinition);
        return null;
    }
}