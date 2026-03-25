using System;
using System.Collections.Generic;

namespace PilotApp.Models;

public sealed class TaskItem
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public string Title       { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Assignee   { get; set; }
    public string? Project    { get; set; }
    public DateTime? DueDate  { get; set; }
    public AppTaskStatus   Status   { get; set; } = AppTaskStatus.New;
    public AppTaskPriority Priority { get; set; } = AppTaskPriority.Medium;
    public List<StatusHistoryEntry> StatusHistory { get; set; } = new();
    public List<TaskComment>        Comments      { get; set; } = new();
}