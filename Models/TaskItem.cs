using System;

namespace PilotApp.Models;

public sealed class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Assignee { get; set; }
    public DateTime? DueDate { get; set; }
    public AppTaskStatus Status { get; set; } = AppTaskStatus.New;
}