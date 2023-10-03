using System.Globalization;
using System.Windows;
using System.Windows.Media;
using AnakinRaW.CommonUtilities.Wpf.Controls;

namespace AnakinRaW.CommonUtilities.Wpf.Converters;

internal class ClipConverter : MultiValueConverter<double, double, CornerRadius, Geometry>
{
    private static readonly SimpleCurvedBorder Border = new();

    protected override Geometry Convert(
        double width,
        double height,
        CornerRadius cornerRadius,
        object parameter,
        CultureInfo culture)
    {
        Border.Width = width;
        Border.Height = height;
        Border.CornerRadius = cornerRadius;
        Border.InvalidateArrange();
        Border.Arrange(new Rect(0.0, 0.0, width, height));
        Geometry borderGeometry = Border.BorderGeometry;
        borderGeometry?.Freeze();
        return borderGeometry;
    }
}