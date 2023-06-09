﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace AnakinRaW.CommonUtilities.Wpf.Converters;

public sealed class BooleanAndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.All(v => v is true);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}