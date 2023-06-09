﻿using System.Globalization;
using AnakinRaW.CommonUtilities.Wpf.Converters;
using AnakinRaW.CommonUtilities.Wpf.Imaging.Controls;

namespace AnakinRaW.CommonUtilities.Wpf.Imaging.Converters;

internal sealed class ActualDpiConverter : ValueConverter<double, double>
{
    public static readonly ActualDpiConverter Instance = new();

    protected override double Convert(double dpi, object? parameter, CultureInfo culture) => AreClose(dpi, 0.0) ? ThemedImage.DefaultDpi : dpi;

    private static bool AreClose(double value1, double value2)
    {
        if (IsNonreal(value1) || IsNonreal(value2))
            return value1.CompareTo(value2) == 0;
        if (value1 == value2)
            return true;
        var num = value1 - value2;
        return num is < 1.53E-06 and > -1.53E-06;
    }

    private static bool IsNonreal(double value)
    {
        return double.IsNaN(value) || double.IsInfinity(value);
    }
}