using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;

namespace PilotApp.ViewModels;

public partial class TimeViewModel : ObservableObject
{
    public ObservableCollection<TimeEntry> Entries { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteEntryCommand))]
    private TimeEntry? selectedEntry;

    [ObservableProperty]
    private string newTaskTitle = string.Empty;

    [ObservableProperty]
    private string newUser = string.Empty;

    [ObservableProperty]
    private DateTime newDate = DateTime.Today;

    [ObservableProperty]
    private double newHours;

    [ObservableProperty]
    private string newComment = string.Empty;

    [ObservableProperty]
    private string filterUser = string.Empty;

    public IEnumerable<TimeEntry> FilteredEntries =>
        Entries.Where(e =>
            string.IsNullOrWhiteSpace(FilterUser) ||
            (e.User ?? "").Contains(FilterUser, StringComparison.OrdinalIgnoreCase));

    public double TotalHours => FilteredEntries.Sum(e => e.Hours);

    public IEnumerable<UserSummary> UserSummaries =>
        Entries
            .GroupBy(e => e.User ?? "—")
            .Select(g => new UserSummary(g.Key, g.Sum(e => e.Hours)))
            .OrderByDescending(s => s.Hours);

    public IEnumerable<TimeEntry> OverdueEntries =>
        Entries.Where(e => e.Date < DateTime.Today.AddDays(-1));

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void AddEntry()
    {
        var entry = new TimeEntry
        {
            TaskTitle = NewTaskTitle.Trim(),
            User      = NewUser.Trim(),
            Date      = NewDate,
            Hours     = NewHours,
            Comment   = string.IsNullOrWhiteSpace(NewComment) ? null : NewComment.Trim()
        };
        Entries.Add(entry);
        RefreshAll();
        NewTaskTitle = string.Empty;
        NewHours     = 0;
        NewComment   = string.Empty;
    }

    private bool CanAdd() =>
        !string.IsNullOrWhiteSpace(NewTaskTitle) && NewHours > 0;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void DeleteEntry()
    {
        if (SelectedEntry is null) return;
        var idx = Entries.IndexOf(SelectedEntry);
        Entries.Remove(SelectedEntry);
        SelectedEntry = Entries.Count == 0
            ? null
            : Entries[Math.Clamp(idx, 0, Entries.Count - 1)];
        RefreshAll();
    }

    private bool CanDelete() => SelectedEntry is not null;
    partial void OnFilterUserChanged(string value) => RefreshAll();
    partial void OnNewTaskTitleChanged(string value) => AddEntryCommand.NotifyCanExecuteChanged();
    partial void OnNewHoursChanged(double value)     => AddEntryCommand.NotifyCanExecuteChanged();

    private void RefreshAll()
    {
        OnPropertyChanged(nameof(FilteredEntries));
        OnPropertyChanged(nameof(TotalHours));
        OnPropertyChanged(nameof(UserSummaries));
        OnPropertyChanged(nameof(OverdueEntries));
    }
    
    public void Load(IEnumerable<TimeEntry> items)
    {
        Entries.Clear();
        foreach (var e in items)
            Entries.Add(e);
        SelectedEntry = Entries.FirstOrDefault();
        RefreshAll();
    }

    public IEnumerable<TimeEntry> Dump() => Entries;
}
public sealed record UserSummary(string User, double Hours);
