using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PilotApp.Converters;

public sealed class AuditSuccessColorConverter : IValueConverter
{
    public static readonly AuditSuccessColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool success && success
            ? new SolidColorBrush(Color.Parse("#43A047"))
            : new SolidColorBrush(Color.Parse("#d32f2f"));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}