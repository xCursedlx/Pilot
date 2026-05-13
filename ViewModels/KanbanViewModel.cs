using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class KanbanViewModel : ObservableObject
{
    private readonly TasksViewModel _tasksVm;
    private UserRole _role = UserRole.Admin;
    private string _currentLogin = string.Empty;

    public KanbanColumn New { get; } = new(AppTaskStatus.New, "Новые");
    public KanbanColumn InProgress { get; } = new(AppTaskStatus.InProgress, "В работе");
    public KanbanColumn OnReview { get; } = new(AppTaskStatus.OnReview, "На проверке");
    public KanbanColumn Done { get; } = new(AppTaskStatus.Done, "Готово");

    public IReadOnlyList<KanbanColumn> Columns { get; }

    [ObservableProperty] private TaskItem? selectedTask;

    public bool CanMove => PermissionService.CanEditTask(_role);

    public KanbanViewModel(TasksViewModel tasksVm)
    {
        _tasksVm = tasksVm;
        Columns = [New, InProgress, OnReview, Done];

        _tasksVm.Tasks.CollectionChanged += OnTasksCollectionChanged;

        foreach (var task in _tasksVm.Tasks)
            task.PropertyChanged += OnTaskPropertyChanged;

        Reload();
    }

    public void SetPermissions(UserRole role, string login)
    {
        _role = role;
        _currentLogin = login;
        OnPropertyChanged(nameof(CanMove));
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

        var tasks = _role == UserRole.User
            ? _tasksVm.Tasks.Where(t =>
                (t.Assignee ?? "").Equals(_currentLogin, System.StringComparison.OrdinalIgnoreCase) ||
                (t.Assignee ?? "").Equals(
                    _tasksVm.Tasks.FirstOrDefault()?.Assignee ?? _currentLogin,
                    System.StringComparison.OrdinalIgnoreCase))
            : _tasksVm.Tasks;

        // Для User фильтруем только свои задачи по отображаемому имени
        var visibleTasks = _role == UserRole.User
            ? _tasksVm.Tasks.Where(t =>
                string.IsNullOrWhiteSpace(t.Assignee) ||
                t.Assignee.Equals(_currentLogin, System.StringComparison.OrdinalIgnoreCase))
            : _tasksVm.Tasks;

        foreach (var task in visibleTasks)
            GetColumn(task.Status)?.Items.Add(task);

        foreach (var col in Columns)
            col.RefreshCount();
    }

    private KanbanColumn? GetColumn(AppTaskStatus status) =>
        Columns.FirstOrDefault(c => c.Status == status);

    [RelayCommand(CanExecute = nameof(CanMove))]
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