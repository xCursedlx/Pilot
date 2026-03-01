using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PilotApp.Converters;

public sealed class DueDateColorConverter : IValueConverter
{
    public static readonly DueDateColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        DateTime? due = value switch
        {
            DateTime dt             => dt,
            DateTimeOffset dto      => dto.DateTime,
            _                       => null
        };
        if (due is null)
            return new SolidColorBrush(Color.Parse("#757575"));
        if (due.Value.Date < DateTime.Today)
            return new SolidColorBrush(Color.Parse("#C62828"));
        if (due.Value.Date == DateTime.Today)
            return new SolidColorBrush(Color.Parse("#E65100"));
        return new SolidColorBrush(Color.Parse("#2E7D32"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}