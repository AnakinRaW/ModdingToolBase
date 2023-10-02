using System.Windows;

namespace AnakinRaW.CommonUtilities.Wpf;

internal static class FluentColors
{
    public static ComponentResourceKey TextFillPrimary { get; } = new(typeof(FluentColors), nameof(TextFillPrimary));
    public static ComponentResourceKey SubtleFillSecondary { get; } = new(typeof(FluentColors), nameof(SubtleFillSecondary));
    public static ComponentResourceKey TextFillSecondary { get; } = new(typeof(FluentColors), nameof(TextFillSecondary));
    public static ComponentResourceKey SubtleFillTertiary { get; } = new(typeof(FluentColors), nameof(SubtleFillTertiary));
    public static ComponentResourceKey AccentFillDefault { get; } = new(typeof(FluentColors), nameof(AccentFillDefault));
}