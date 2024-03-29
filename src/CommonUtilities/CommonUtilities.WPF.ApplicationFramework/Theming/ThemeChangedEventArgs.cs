using System;

namespace AnakinRaW.CommonUtilities.Wpf.ApplicationFramework.Theming;

/// <summary>
/// The event args when <see cref="IThemeManager.ThemeChanged"/> was raised.
/// </summary>
public class ThemeChangedEventArgs(ITheme newTheme, ITheme oldTheme) : EventArgs
{
    /// <summary>
    /// The new theme.
    /// </summary>
    public ITheme NewTheme { get; } = newTheme;

    /// <summary>
    /// The old theme.
    /// </summary>
    public ITheme OldTheme { get; } = oldTheme;
}