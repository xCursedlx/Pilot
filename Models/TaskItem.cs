using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace PilotApp.Models;

public sealed partial class TaskItem : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string? description;
    [ObservableProperty] private string? assignee;
    [ObservableProperty] private string? project;
    [ObservableProperty] private DateTime? dueDate;
    [ObservableProperty] private AppTaskStatus status = AppTaskStatus.New;
    [ObservableProperty] private AppTaskPriority priority = AppTaskPriority.Medium;

    public List<StatusHistoryEntry> StatusHistory { get; set; } = new();
    public List<TaskComment> Comments { get; set; } = new();
}