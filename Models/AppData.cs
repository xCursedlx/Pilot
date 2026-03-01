using System.Collections.Generic;

namespace PilotApp.Models;

public sealed class AppData
{
    public List<TaskItem> Tasks { get; set; } = new();
    public List<DocumentItem> Documents { get; set; } = new();
    public List<TimeEntry> TimeEntries { get; set; } = new();
}