using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PilotApp.Models;

namespace PilotApp.Converters;

public sealed class TaskStatusColorConverter : IValueConverter
{
    public static readonly TaskStatusColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AppTaskStatus status)
            return status switch
            {
                AppTaskStatus.New => new SolidColorBrush(Color.Parse("#78909C")),
                AppTaskStatus.InProgress => new SolidColorBrush(Color.Parse("#1E88E5")),
                AppTaskStatus.OnReview => new SolidColorBrush(Color.Parse("#FB8C00")),
                AppTaskStatus.Done => new SolidColorBrush(Color.Parse("#43A047")),
                _ => new SolidColorBrush(Color.Parse("#9E9E9E"))
            };

        return new SolidColorBrush(Color.Parse("#9E9E9E"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}