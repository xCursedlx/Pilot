using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PilotApp.Models;

namespace PilotApp.Converters;

public sealed class TaskPriorityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AppTaskPriority p) return null;

        if (parameter is string s && s == "color")
            return p switch
            {
                AppTaskPriority.Low      => new SolidColorBrush(Color.Parse("#2e7d32")),
                AppTaskPriority.Medium   => new SolidColorBrush(Color.Parse("#1565c0")),
                AppTaskPriority.High     => new SolidColorBrush(Color.Parse("#e65100")),
                AppTaskPriority.Critical => new SolidColorBrush(Color.Parse("#b71c1c")),
                _                        => Brushes.Gray
            };

        return p switch
        {
            AppTaskPriority.Low      => "Низкий",
            AppTaskPriority.Medium   => "Средний",
            AppTaskPriority.High     => "Высокий",
            AppTaskPriority.Critical => "Критический",
            _                        => p.ToString()
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}