﻿using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using AnakinRaW.CommonUtilities.Wpf.Converters;
using AnakinRaW.CommonUtilities.Wpf.Utilities;

namespace AnakinRaW.CommonUtilities.Wpf.Controls;

public class ThemedContextMenu : ContextMenu
{
    public event EventHandler? IsCommandInExecutionChanged;

    public static readonly DependencyProperty PopupAnimationProperty = Popup.PopupAnimationProperty.AddOwner(typeof(ThemedContextMenu));

    private static readonly DependencyPropertyKey IsInsideContextMenuPropertyKey =
        DependencyProperty.RegisterAttachedReadOnly("IsInsideContextMenu", typeof(bool), typeof(ThemedContextMenu),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static readonly DependencyProperty ShowOptionsProperty = DependencyProperty.Register(nameof(ShowOptions), typeof(MenuShowOptions), typeof(ThemedContextMenu), new FrameworkPropertyMetadata((MenuShowOptions)0));

    public static readonly DependencyProperty UseFilterProperty = DependencyProperty.Register(nameof(UseFilter),
        typeof(bool), typeof(ThemedContextMenu), new FrameworkPropertyMetadata(true));

    public static readonly DependencyProperty IsInsideContextMenuProperty = IsInsideContextMenuPropertyKey.DependencyProperty;
    public static readonly DependencyProperty? ShowKeyboardCuesProperty = FetchShowKeyboardCuesProperty();

    private static readonly BrushToColorConverter BrushToColorConverter = new();

    private bool _isCommandInExecution;
    private ScrollViewer? _scrollViewer;
    private double _horizontalOffset = double.MinValue;

    public MenuShowOptions ShowOptions
    {
        get => (MenuShowOptions)GetValue(ShowOptionsProperty);
        set
        {
            SetValue(ShowOptionsProperty, value);
            UpdatePlacementMode(value);
        }
    }

    public PopupAnimation PopupAnimation
    {
        get => (PopupAnimation)GetValue(PopupAnimationProperty);
        set => SetValue(PopupAnimationProperty, value);
    }
        
    public bool UseFilter
    {
        get => (bool)GetValue(UseFilterProperty);
        set => SetValue(UseFilterProperty, value);
    }

    public bool IsCommandInExecution
    {
        get => _isCommandInExecution;
        private set
        {
            if (_isCommandInExecution == value)
                return;
            _isCommandInExecution = value;
            OnIsCommandInExecutionChanged(EventArgs.Empty);
        }
    }
    
    public static bool GetIsInsideContextMenu(DependencyObject element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));
        return (bool)element.GetValue(IsInsideContextMenuProperty);
    }

    private static void SetIsInsideContextMenu(DependencyObject element, bool value)
    {
        if (element == null) 
            throw new ArgumentNullException(nameof(element));
        element.SetValue(IsInsideContextMenuPropertyKey, value);
    }

    private bool AreUnderlinesShown => ShowOptions.HasFlag(MenuShowOptions.ShowMnemonics);

    private bool IsFirstItemSelected => ShowOptions.HasFlag(MenuShowOptions.SelectFirstItem);

    private bool IsTypeAheadSupported => ShowOptions.HasFlag(MenuShowOptions.SupportsTypeAhead);

    private bool LeftAligned => ShowOptions.HasFlag(MenuShowOptions.LeftAlign);

    private bool RightAligned => !LeftAligned && ShowOptions.HasFlag(MenuShowOptions.RightAlign);

    static ThemedContextMenu()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedContextMenu), new FrameworkPropertyMetadata(typeof(ThemedContextMenu)));
    }

    public ThemedContextMenu()
    {
        SetIsInsideContextMenu(this, true);
        AddHandler(ThemedMenuItem.CommandExecutedRoutedEvent, new RoutedEventHandler(BeforeCommandExecution), true);
        AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(AfterCommandExecution), true);
        PresentationSource.AddSourceChangedHandler(this, OnSourceChanged);
        SetupFilter();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new ThemedMenuItem();
    }

    protected virtual void OnIsCommandInExecutionChanged(EventArgs e)
    {
        IsCommandInExecutionChanged?.Invoke(this, e);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property != ShowKeyboardCuesProperty || (bool)e.NewValue || !AreUnderlinesShown)
            return;
        SetValue(ShowKeyboardCuesProperty, true);
    }

    protected override void OnOpened(RoutedEventArgs e)
    {
        if (_horizontalOffset == double.MinValue)
            _horizontalOffset = HorizontalOffset;
        if (LeftAligned && SystemParameters.MenuDropAlignment)
            HorizontalOffset = ActualWidth - _horizontalOffset;
        else if (RightAligned && !SystemParameters.MenuDropAlignment)
            HorizontalOffset = -ActualWidth - _horizontalOffset;
        SetupFilter();
        _scrollViewer?.ScrollToVerticalOffset(0.0);
        base.OnOpened(e);
        ProcessShowOptions();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        MenuUtilities.ProcessForDirectionalNavigation(e, this, Orientation.Vertical);
    }

    private static DependencyProperty? FetchShowKeyboardCuesProperty()
    {
        var field = typeof(KeyboardNavigation).GetField("ShowKeyboardCuesProperty", BindingFlags.Static | BindingFlags.NonPublic);
        return field == null ? null : field.GetValue(null) as DependencyProperty;
    }

    private void BeforeCommandExecution(object sender, RoutedEventArgs e)
    {
        IsCommandInExecution = true;
    }

    private void AfterCommandExecution(object sender, RoutedEventArgs e)
    {
        IsCommandInExecution = false;
    }

    private void OnSourceChanged(object sender, SourceChangedEventArgs e)
    {
        if (e.NewSource == null || LogicalTreeHelper.GetParent(this) is not Popup parent)
            return;
        var binding = new Binding
        {
            Source = this,
            Path = new PropertyPath(PopupAnimationProperty)
        };
        parent.SetBinding(Popup.PopupAnimationProperty, binding);
    }

    private void ProcessShowOptions()
    {
        if (IsFirstItemSelected)
        {
            for (var index = 0; index < Items.Count; ++index)
            {
                if (ItemContainerGenerator.ContainerFromIndex(index) is UIElement
                    {
                        Visibility: Visibility.Visible, IsEnabled: true, Focusable: true
                    } element)
                {
                    Keyboard.Focus(element);
                    break;
                }
            }
        }
        IsTextSearchEnabled = IsTypeAheadSupported;
        if (!AreUnderlinesShown || ShowKeyboardCuesProperty == null)
            return;
        SetValue(ShowKeyboardCuesProperty, true);
    }


    private void UpdatePlacementMode(MenuShowOptions options)
    {
        if (options.HasFlag(MenuShowOptions.SelectFirstItem))
            Placement = PlacementMode.Top;
        else if (options.HasFlag(MenuShowOptions.PlaceBottom))
            Placement = PlacementMode.Bottom;
        else
            Placement = PlacementMode.Relative;
    }

    private void SetupFilter()
    {
        Items.Filter = UseFilter ? FilterNonVisibleItems : null;
    }

    protected virtual bool FilterNonVisibleItems(object item)
    {
        return true;
    }
}