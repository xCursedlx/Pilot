using System;
using System.Globalization;
using Avalonia.Data.Converters;
using PilotApp.Models;

namespace PilotApp.Converters;

public sealed class TaskStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is AppTaskStatus s ? s switch
        {
            AppTaskStatus.New        => "Новая",
            AppTaskStatus.InProgress => "В работе",
            AppTaskStatus.OnReview   => "На проверке",
            AppTaskStatus.Done       => "Готово",
            _                        => value.ToString()
        } : value?.ToString() ?? "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}