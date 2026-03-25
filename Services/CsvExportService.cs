using System.Collections.Generic;
using System.IO;
using System.Text;
using PilotApp.Models;

namespace PilotApp.Services;

public static class CsvExportService
{
    public static void ExportTasks(IEnumerable<TaskItem> tasks, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id;Название;Исполнитель;Проект;Статус;Приоритет;Срок;Описание");
        foreach (var t in tasks)
        {
            sb.AppendLine(string.Join(";",
                t.Id,
                Escape(t.Title),
                Escape(t.Assignee),
                Escape(t.Project),
                t.Status,
                t.Priority,
                t.DueDate?.ToString("dd.MM.yyyy") ?? "",
                Escape(t.Description)));
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public static void ExportTimeEntries(IEnumerable<TimeEntry> entries, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id;Задача;Исполнитель;Дата;Часы;Комментарий");
        foreach (var e in entries)
        {
            sb.AppendLine(string.Join(";",
                e.Id,
                Escape(e.TaskTitle),
                Escape(e.User),
                e.Date.ToString("dd.MM.yyyy"),
                e.Hours.ToString("0.##"),
                Escape(e.Comment)));
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public static void ExportDocuments(IEnumerable<DocumentItem> docs, string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id;Название;Версия;Путь к файлу;Создан;Описание");
        foreach (var d in docs)
        {
            sb.AppendLine(string.Join(";",
                d.Id,
                Escape(d.Name),
                Escape(d.Version),
                Escape(d.FilePath),
                d.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                Escape(d.Description)));
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}