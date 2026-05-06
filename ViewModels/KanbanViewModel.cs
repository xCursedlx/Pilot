using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;

namespace PilotApp.ViewModels;

public partial class KanbanViewModel : ObservableObject
{
    private readonly TasksViewModel _tasksVm;

    public KanbanColumn New { get; } = new(AppTaskStatus.New, "Новые");
    public KanbanColumn InProgress { get; } = new(AppTaskStatus.InProgress, "В работе");
    public KanbanColumn OnReview { get; } = new(AppTaskStatus.OnReview, "На проверке");
    public KanbanColumn Done { get; } = new(AppTaskStatus.Done, "Готово");

    public IReadOnlyList<KanbanColumn> Columns { get; }

    [ObservableProperty] private TaskItem? selectedTask;

    public KanbanViewModel(TasksViewModel tasksVm)
    {
        _tasksVm = tasksVm;
        Columns = new[] { New, InProgress, OnReview, Done };

        _tasksVm.Tasks.CollectionChanged += OnTasksCollectionChanged;

        foreach (var task in _tasksVm.Tasks)
            task.PropertyChanged += OnTaskPropertyChanged;

        Reload();
    }

    private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (TaskItem t in e.NewItems)
                t.PropertyChanged += OnTaskPropertyChanged;

        if (e.OldItems != null)
            foreach (TaskItem t in e.OldItems)
                t.PropertyChanged -= OnTaskPropertyChanged;

        Reload();
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TaskItem.Status) or nameof(TaskItem.Priority))
            Reload();
    }

    public void Reload()
    {
        foreach (var col in Columns)
            col.Items.Clear();

        foreach (var task in _tasksVm.Tasks)
            GetColumn(task.Status)?.Items.Add(task);

        foreach (var col in Columns)
            col.RefreshCount();
    }

    private KanbanColumn? GetColumn(AppTaskStatus status) =>
        Columns.FirstOrDefault(c => c.Status == status);

    [RelayCommand]
    private void MoveTask((TaskItem task, AppTaskStatus target) args)
    {
        var (task, target) = args;
        if (task.Status == target) return;

        GetColumn(task.Status)?.Items.Remove(task);
        _tasksVm.ChangeStatus(task, target);
        GetColumn(target)?.Items.Add(task);

        foreach (var col in Columns)
            col.RefreshCount();
    }
}

public partial class KanbanColumn : ObservableObject
{
    public AppTaskStatus Status { get; }
    public string Title { get; }
    public ObservableCollection<TaskItem> Items { get; } = new();

    [ObservableProperty] private string countLabel = "";

    public KanbanColumn(AppTaskStatus status, string title)
    {
        Status = status;
        Title = title;
    }

    public void RefreshCount() =>
        CountLabel = Items.Count > 0 ? $"{Items.Count}" : "";
}