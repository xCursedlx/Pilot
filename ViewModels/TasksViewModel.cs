using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class TasksViewModel : ObservableObject
{
    private Action? _markDirty;
    private IDialogService? _dialog;

    public void SetDirtyCallback(Action markDirty) => _markDirty = markDirty;
    public void SetDialogService(IDialogService dialog) => _dialog = dialog;

    public ObservableCollection<TaskItem> Tasks { get; } = new();

    public IReadOnlyList<AppTaskStatus> Statuses { get; } = Enum.GetValues<AppTaskStatus>();
    public IReadOnlyList<AppTaskPriority> Priorities { get; } = Enum.GetValues<AppTaskPriority>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteTaskCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveTaskCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddCommentCommand))]
    private TaskItem? selectedTask;

    partial void OnSelectedTaskChanged(TaskItem? value)
    {
        NewCommentText = string.Empty;
        OnPropertyChanged(nameof(SelectedComments));
        OnPropertyChanged(nameof(SelectedStatusHistory));
    }

    [ObservableProperty] private string filterText = string.Empty;
    [ObservableProperty] private AppTaskStatus? filterStatus;
    [ObservableProperty] private string filterProject = string.Empty;

    public IReadOnlyList<AppTaskStatus?> FilterStatuses { get; } =
        new AppTaskStatus?[] { null }
            .Concat(Enum.GetValues<AppTaskStatus>().Cast<AppTaskStatus?>())
            .ToList();

    public IEnumerable<TaskItem> FilteredTasks =>
        Tasks.Where(t =>
            (string.IsNullOrWhiteSpace(FilterText) ||
             t.Title.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
             (t.Assignee ?? "").Contains(FilterText, StringComparison.OrdinalIgnoreCase)) &&
            (FilterStatus is null || t.Status == FilterStatus) &&
            (string.IsNullOrWhiteSpace(FilterProject) ||
             (t.Project ?? "").Contains(FilterProject, StringComparison.OrdinalIgnoreCase)));

    public IEnumerable<TaskComment> SelectedComments => SelectedTask?.Comments ?? Enumerable.Empty<TaskComment>();
    public IEnumerable<StatusHistoryEntry> SelectedStatusHistory => SelectedTask?.StatusHistory ?? Enumerable.Empty<StatusHistoryEntry>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommentCommand))]
    private string newCommentText = string.Empty;

    [ObservableProperty] private string newCommentAuthor = string.Empty;

    [RelayCommand]
    private void AddTask()
    {
        var t = new TaskItem
        {
            Title = "Новая задача",
            DueDate = DateTime.Today.AddDays(7),
            Priority = AppTaskPriority.Medium
        };
        Tasks.Add(t);
        SelectedTask = t;
        _markDirty?.Invoke();
        OnFilterChanged();
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task DeleteTask()
    {
        if (SelectedTask is null) return;
        if (_dialog is not null)
        {
            var ok = await _dialog.ConfirmAsync(
                "Удаление задачи",
                $"Удалить задачу «{SelectedTask.Title}»?\nЭто действие нельзя отменить.");
            if (!ok) return;
        }
        var idx = Tasks.IndexOf(SelectedTask);
        Tasks.Remove(SelectedTask);
        SelectedTask = Tasks.Count == 0
            ? null
            : Tasks[Math.Clamp(idx, 0, Tasks.Count - 1)];
        _markDirty?.Invoke();
        OnFilterChanged();
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private void SaveTask()
    {
        _markDirty?.Invoke();
        OnFilterChanged();
    }

    public void ChangeStatus(TaskItem task, AppTaskStatus newStatus, string? changedBy = null)
    {
        if (task.Status == newStatus) return;
        task.StatusHistory.Add(new StatusHistoryEntry
        {
            OldStatus = task.Status,
            NewStatus = newStatus,
            ChangedAt = DateTime.Now,
            ChangedBy = changedBy
        });
        task.Status = newStatus;
        _markDirty?.Invoke();
        OnPropertyChanged(nameof(SelectedStatusHistory));
        OnFilterChanged();
    }

    [RelayCommand(CanExecute = nameof(CanAddComment))]
    private void AddComment()
    {
        if (SelectedTask is null) return;
        SelectedTask.Comments.Add(new TaskComment
        {
            Author = string.IsNullOrWhiteSpace(NewCommentAuthor) ? "Аноним" : NewCommentAuthor.Trim(),
            Text = NewCommentText.Trim(),
            CreatedAt = DateTime.Now
        });
        NewCommentText = string.Empty;
        _markDirty?.Invoke();
        OnPropertyChanged(nameof(SelectedComments));
    }

    private bool CanModify() => SelectedTask is not null;
    private bool CanAddComment() => SelectedTask is not null && !string.IsNullOrWhiteSpace(NewCommentText);

    partial void OnFilterTextChanged(string value) => OnFilterChanged();
    partial void OnFilterStatusChanged(AppTaskStatus? value) => OnFilterChanged();
    partial void OnFilterProjectChanged(string value) => OnFilterChanged();
    partial void OnNewCommentTextChanged(string value) => AddCommentCommand.NotifyCanExecuteChanged();

    private void OnFilterChanged() => OnPropertyChanged(nameof(FilteredTasks));

    public void Load(IEnumerable<TaskItem> items)
    {
        Tasks.Clear();
        foreach (var t in items) Tasks.Add(t);
        SelectedTask = Tasks.FirstOrDefault();
        OnFilterChanged();
    }

    public IEnumerable<TaskItem> Dump() => Tasks;
}