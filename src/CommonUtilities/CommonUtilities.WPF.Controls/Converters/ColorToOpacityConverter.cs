using System.Globalization;
using System.Windows.Media;

namespace AnakinRaW.CommonUtilities.Wpf.Converters;

internal class ColorToOpacityConverter : ValueConverter<object, double>
{
    protected override double Convert(object colorObj, object? parameter, CultureInfo culture)
    {
        return colorObj is not Color nullable ? 1.0 : nullable.A / (double)byte.MaxValue;
    }
}