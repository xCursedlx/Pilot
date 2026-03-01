using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;

namespace PilotApp.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    public ObservableCollection<TaskItem> Tasks { get; } = new();

    public IReadOnlyList<AppTaskStatus> Statuses { get; } =
        Enum.GetValues<AppTaskStatus>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteTaskCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    private TaskItem? selectedTask;
    
    [ObservableProperty]
    private string filterText = string.Empty;

    [ObservableProperty]
    private AppTaskStatus? filterStatus;

    public IReadOnlyList<AppTaskStatus?> FilterStatuses { get; } =
        new AppTaskStatus?[] { null }
            .Concat(Enum.GetValues<AppTaskStatus>().Cast<AppTaskStatus?>())
            .ToList();

    public IEnumerable<TaskItem> FilteredTasks =>
        Tasks.Where(t =>
            (string.IsNullOrWhiteSpace(FilterText) ||
             t.Title.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
             (t.Assignee ?? "").Contains(FilterText, StringComparison.OrdinalIgnoreCase)) &&
            (FilterStatus is null || t.Status == FilterStatus));

    [RelayCommand]
    private void AddTask()
    {
        var t = new TaskItem
        {
            Title = "Новая задача",
            DueDate = DateTime.Today.AddDays(7)
        };
        Tasks.Add(t);
        SelectedTask = t;
        OnFilterChanged();
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private void DeleteTask()
    {
        if (SelectedTask is null) return;
        var idx = Tasks.IndexOf(SelectedTask);
        Tasks.Remove(SelectedTask);
        SelectedTask = Tasks.Count == 0
            ? null
            : Tasks[Math.Clamp(idx, 0, Tasks.Count - 1)];
        OnFilterChanged();
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private void SaveTask()
    {
        OnFilterChanged();
    }

    private bool CanModify() => SelectedTask is not null;
    partial void OnFilterTextChanged(string value) => OnFilterChanged();
    partial void OnFilterStatusChanged(AppTaskStatus? value) => OnFilterChanged();

    private void OnFilterChanged() =>
        OnPropertyChanged(nameof(FilteredTasks));
    
    public void Load(IEnumerable<TaskItem> items)
    {
        Tasks.Clear();
        foreach (var t in items)
            Tasks.Add(t);
        SelectedTask = Tasks.FirstOrDefault();
        OnFilterChanged();
    }

    public IEnumerable<TaskItem> Dump() => Tasks;
}
