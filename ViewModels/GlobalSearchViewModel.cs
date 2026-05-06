using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;

namespace PilotApp.ViewModels;

public partial class GlobalSearchViewModel : ObservableObject
{
    private readonly TasksViewModel _tasks;
    private readonly DocumentsViewModel _documents;
    private readonly TimeViewModel _time;

    public ObservableCollection<SearchResult> Results { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
    private string query = string.Empty;

    [ObservableProperty] private bool isVisible;
    [ObservableProperty] private int totalCount;

    public GlobalSearchViewModel(
        TasksViewModel tasks,
        DocumentsViewModel documents,
        TimeViewModel time)
    {
        _tasks = tasks;
        _documents = documents;
        _time = time;
    }

    public void Show()
    {
        IsVisible = true;
        Query = string.Empty;
        Results.Clear();
        TotalCount = 0;
    }

    public void CloseSearch() => IsVisible = false;

    [RelayCommand(CanExecute = nameof(CanSearch))]
    private void Search()
    {
        Results.Clear();
        if (string.IsNullOrWhiteSpace(Query)) return;

        var q = Query.Trim();

        foreach (var t in _tasks.Tasks)
        {
            if (Matches(t.Title, q) || Matches(t.Description, q) ||
                Matches(t.Assignee, q) || Matches(t.Project, q))
            {
                Results.Add(new SearchResult(
                    "Задача",
                    t.Title,
                    $"{t.Status} - {t.Priority}" +
                    (t.Project is not null ? $" - {t.Project}" : ""),
                    ResultKind.Task, t));
            }

            foreach (var c in t.Comments)
            {
                if (Matches(c.Text, q) || Matches(c.Author, q))
                {
                    Results.Add(new SearchResult(
                        "Комментарий",
                        $"{t.Title} — {c.Author}",
                        c.Text.Length > 80 ? c.Text[..80] + "..." : c.Text,
                        ResultKind.Comment, t));
                }
            }
        }

        foreach (var d in _documents.Documents)
        {
            if (Matches(d.Name, q) || Matches(d.Description, q) || Matches(d.Version, q))
            {
                Results.Add(new SearchResult(
                    "Документ",
                    d.Name,
                    $"{d.Version} - {d.CreatedAt:dd.MM.yyyy}",
                    ResultKind.Document, d));
            }
        }

        foreach (var e in _time.Entries)
        {
            if (Matches(e.TaskTitle, q) || Matches(e.User, q) || Matches(e.Comment, q))
            {
                Results.Add(new SearchResult(
                    "Время",
                    e.TaskTitle,
                    $"{e.User} - {e.Date:dd.MM.yyyy} - {e.Hours:0.#} ч",
                    ResultKind.TimeEntry, e));
            }
        }

        TotalCount = Results.Count;
    }

    private bool CanSearch() => !string.IsNullOrWhiteSpace(Query);

    partial void OnQueryChanged(string value)
    {
        if (value.Length >= 2)
            Search();
        else if (value.Length == 0)
        {
            Results.Clear();
            TotalCount = 0;
        }
    }

    private static bool Matches(string? text, string query) =>
        text is not null &&
        text.Contains(query, StringComparison.OrdinalIgnoreCase);
}

public enum ResultKind { Task, Comment, Document, TimeEntry }

public sealed record SearchResult(
    string Category,
    string Title,
    string Subtitle,
    ResultKind Kind,
    object Source);