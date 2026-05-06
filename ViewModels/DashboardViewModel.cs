using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PilotApp.Models;

namespace PilotApp.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly TasksViewModel _tasks;
    private readonly DocumentsViewModel _documents;
    private readonly TimeViewModel _time;

    public DashboardViewModel(
        TasksViewModel tasks,
        DocumentsViewModel documents,
        TimeViewModel time)
    {
        _tasks = tasks;
        _documents = documents;
        _time = time;

        _tasks.Tasks.CollectionChanged += (_, _) => Refresh();
        _documents.Documents.CollectionChanged += (_, _) => Refresh();
        _time.Entries.CollectionChanged += (_, _) => Refresh();
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(TotalTasks));
        OnPropertyChanged(nameof(InProgressCount));
        OnPropertyChanged(nameof(OverdueCount));
        OnPropertyChanged(nameof(TotalDocuments));
        OnPropertyChanged(nameof(TotalHours));
        OnPropertyChanged(nameof(StatusSummaries));
        OnPropertyChanged(nameof(TopAssignees));
        OnPropertyChanged(nameof(UpcomingDeadlines));
        OnPropertyChanged(nameof(HasNoAssignees));
        OnPropertyChanged(nameof(HasNoUpcomingDeadlines));
    }

    public int TotalTasks => _tasks.Tasks.Count;
    public int InProgressCount => _tasks.Tasks.Count(t => t.Status == AppTaskStatus.InProgress);
    public int OverdueCount => _tasks.Tasks.Count(t =>
        t.DueDate.HasValue &&
        t.DueDate.Value.Date < DateTime.Today &&
        t.Status != AppTaskStatus.Done);
    public int TotalDocuments => _documents.Documents.Count;
    public double TotalHours => _time.Entries.Sum(e => e.Hours);

    public IEnumerable<StatusSummary> StatusSummaries =>
        Enum.GetValues<AppTaskStatus>()
            .Select(s => new StatusSummary(
                s switch
                {
                    AppTaskStatus.New => "Новые",
                    AppTaskStatus.InProgress => "В работе",
                    AppTaskStatus.OnReview => "На проверке",
                    AppTaskStatus.Done => "Выполнено",
                    _ => s.ToString()
                },
                _tasks.Tasks.Count(t => t.Status == s),
                s switch
                {
                    AppTaskStatus.New => "#78909C",
                    AppTaskStatus.InProgress => "#1E88E5",
                    AppTaskStatus.OnReview => "#FB8C00",
                    AppTaskStatus.Done => "#43A047",
                    _ => "#9E9E9E"
                }));

    public IEnumerable<AssigneeSummary> TopAssignees =>
        _time.Entries
            .GroupBy(e => e.User ?? "—")
            .Select(g => new AssigneeSummary(g.Key, g.Sum(e => e.Hours)))
            .OrderByDescending(a => a.Hours)
            .Take(5);

    public bool HasNoAssignees => !TopAssignees.Any();
    public bool HasNoUpcomingDeadlines => !UpcomingDeadlines.Any();

    public IEnumerable<TaskItem> UpcomingDeadlines =>
        _tasks.Tasks
            .Where(t =>
                t.DueDate.HasValue &&
                t.DueDate.Value.Date >= DateTime.Today &&
                t.DueDate.Value.Date <= DateTime.Today.AddDays(7) &&
                t.Status != AppTaskStatus.Done)
            .OrderBy(t => t.DueDate);
}

public sealed record StatusSummary(string Label, int Count, string Color);
public sealed record AssigneeSummary(string Name, double Hours);