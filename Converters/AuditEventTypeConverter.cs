using System;
using System.Globalization;
using Avalonia.Data.Converters;
using PilotApp.Models;

namespace PilotApp.Converters;

public sealed class AuditEventTypeConverter : IValueConverter
{
    public static readonly AuditEventTypeConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AuditEventType t)
            return t switch
            {
                AuditEventType.LoginSuccess => "Вход выполнен",
                AuditEventType.LoginFailed => "Вход не выполнен",
                AuditEventType.Logout => "Выход",
                AuditEventType.TaskCreated => "Задача создана",
                AuditEventType.TaskDeleted => "Задача удалена",
                AuditEventType.DocumentCreated => "Документ создан",
                AuditEventType.DocumentDeleted => "Документ удалён",
                AuditEventType.TimeEntryCreated => "Время добавлено",
                AuditEventType.TimeEntryDeleted => "Время удалено",
                AuditEventType.UserCreated => "Пользователь создан",
                AuditEventType.UserDeleted => "Пользователь удалён",
                _ => t.ToString()
            };
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}