using System.Windows;

namespace AnakinRaW.CommonUtilities.Wpf;

internal static class FluentColors
{
    public static ComponentResourceKey TextFillPrimary { get; } = new(typeof(FluentColors), nameof(TextFillPrimary));
    public static ComponentResourceKey SubtleFillSecondary { get; } = new(typeof(FluentColors), nameof(SubtleFillSecondary));
    public static ComponentResourceKey TextFillSecondary { get; } = new(typeof(FluentColors), nameof(TextFillSecondary));
    public static ComponentResourceKey SubtleFillTertiary { get; } = new(typeof(FluentColors), nameof(SubtleFillTertiary));
    public static ComponentResourceKey AccentFillDefault { get; } = new(typeof(FluentColors), nameof(AccentFillDefault));

    public static ComponentResourceKey SolidBackgroundFillTertiary { get; } = new(typeof(FluentColors), nameof(SolidBackgroundFillTertiary));
    public static ComponentResourceKey SolidBackgroundFillSecondary { get; } = new(typeof(FluentColors), nameof(SolidBackgroundFillSecondary));
    public static ComponentResourceKey FocusStrokeOuter { get; } = new(typeof(FluentColors), nameof(FocusStrokeOuter));
    public static ComponentResourceKey TextFillDisabled { get; } = new(typeof(FluentColors), nameof(TextFillDisabled));
    public static ComponentResourceKey PopupShadow { get; } = new(typeof(FluentColors), nameof(PopupShadow));
    public static ComponentResourceKey PopupBorder { get; } = new(typeof(FluentColors), nameof(PopupBorder));
}