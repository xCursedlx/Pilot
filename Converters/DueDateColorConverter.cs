using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PilotApp.Converters;

public sealed class DueDateColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dt) return null;

        if (dt.Date < DateTime.Today)
            return new SolidColorBrush(Color.Parse("#d32f2f"));
        if (dt.Date <= DateTime.Today.AddDays(2))
            return new SolidColorBrush(Color.Parse("#f57c00"));
        return new SolidColorBrush(Color.Parse("#388e3c"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}