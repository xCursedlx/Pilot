using System;
using System.Globalization;
using Avalonia.Data.Converters;
using PilotApp.Models;

namespace PilotApp.Converters;

public sealed class TaskStatusConverter : IValueConverter
{
    public static readonly TaskStatusConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AppTaskStatus status)
            return status switch
            {
                AppTaskStatus.New        => "Новая",
                AppTaskStatus.InProgress => "В работе",
                AppTaskStatus.OnReview   => "На проверке",
                AppTaskStatus.Done       => "Выполнена",
                _                        => value.ToString() ?? string.Empty
            };

        if (value is null)
            return "Все статусы";

        return value.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}