using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PilotApp.Converters;

public sealed class BoolToStringConverter : IValueConverter
{
    public static readonly BoolToStringConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter as string ?? "Да|Нет").Split('|');
        var trueVal = parts.Length > 0 ? parts[0] : "Да";
        var falseVal = parts.Length > 1 ? parts[1] : "Нет";
        return value is bool b && b ? trueVal : falseVal;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}