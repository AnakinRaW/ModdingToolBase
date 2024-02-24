using System.Windows;

namespace AnakinRaW.CommonUtilities.Wpf.Styles;

internal static class StyleConstants
{
    public const double NonClientHeight = 30.0;

    public const double PopupShadowOffset = 16.0;
    public const double PopupShadowOffsetInvert = -16.0;
    public const double PopupShadowSize = 16.0;

    public const double ControlMinHeightSmall = 24.0;
    public const double ControlMinWidthSmall = 24.0;

    public static readonly Thickness SystemMenuPadding = new(5.0, 5.0, 0.0, 5.0);
    public static readonly Thickness ControlMarginXSmall = new(1.0);
    public static readonly Thickness ControlMarginSmall = new(2.0);
    public static readonly Thickness FocusVisualStrokeThickness = new(2.0);
    public static readonly Thickness FocusVisualMargin = new(-3.0);
    public static readonly Thickness PopupMargin = new(16.0);
    public static readonly Thickness ControlStrokeThickness = new(1.0);

    public static readonly CornerRadius PopupBorderCornerRadius = new(6.0);
    public static readonly CornerRadius ControlCornerRadius = new(4.0);
    public static readonly CornerRadius FocusVisualCornerRadius = new(6.0);

    public static readonly GridLength TextSpacerGridColumnWidth = new(24.0);
}