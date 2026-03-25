using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IRepository _repo;
    private readonly Timer       _autoSaveTimer;
    private bool                 _isDirty;

    public TasksViewModel        Tasks        { get; } = new();
    public DocumentsViewModel    Documents    { get; } = new();
    public TimeViewModel         Time         { get; } = new();
    public KanbanViewModel       Kanban       { get; }
    public GlobalSearchViewModel GlobalSearch { get; }

    [ObservableProperty] private string statusMessage = "Готово";
    [ObservableProperty] private bool   isBusy;
    [ObservableProperty] private bool   isDarkTheme = true;

    partial void OnIsDarkThemeChanged(bool value) => ApplyTheme(value);

    private static void ApplyTheme(bool dark)
    {
        if (Avalonia.Application.Current is not App app) return;
        app.RequestedThemeVariant = dark
            ? Avalonia.Styling.ThemeVariant.Dark
            : Avalonia.Styling.ThemeVariant.Light;
    }

    public string TasksHeader =>
        Tasks.Tasks.Count > 0 ? $"Задачи ({Tasks.Tasks.Count})" : "Задачи";

    public string DocumentsHeader =>
        Documents.Documents.Count > 0 ? $"Документы ({Documents.Documents.Count})" : "Документы";

    public string TimeHeader =>
        Time.Entries.Count > 0 ? $"Время ({Time.Entries.Count})" : "Время";

    public string KanbanHeader => "Канбан";

    public int OverdueTasksCount =>
        Tasks.Tasks.Count(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value.Date < DateTime.Today &&
            t.Status != Models.AppTaskStatus.Done);

    public bool HasOverdueTasks => OverdueTasksCount > 0;

    public MainWindowViewModel(IRepository repo)
    {
        _repo        = repo;
        Kanban       = new KanbanViewModel(Tasks);
        GlobalSearch = new GlobalSearchViewModel(Tasks, Documents, Time);

        Time.SetTasksSource(Tasks);

        Tasks.SetDirtyCallback(MarkDirty);
        Documents.SetDirtyCallback(MarkDirty);
        Time.SetDirtyCallback(MarkDirty);

        var dialog = new DialogService();
        Tasks.SetDialogService(dialog);
        Documents.SetDialogService(dialog);
        Time.SetDialogService(dialog);

        Tasks.Tasks.CollectionChanged         += (_, _) => RefreshCounters();
        Documents.Documents.CollectionChanged += (_, _) => RefreshCounters();
        Time.Entries.CollectionChanged        += (_, _) => RefreshCounters();

        _autoSaveTimer = new Timer(
            async _ => await AutoSaveAsync(),
            null,
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(3));
    }

    private void RefreshCounters()
    {
        OnPropertyChanged(nameof(TasksHeader));
        OnPropertyChanged(nameof(DocumentsHeader));
        OnPropertyChanged(nameof(TimeHeader));
        OnPropertyChanged(nameof(OverdueTasksCount));
        OnPropertyChanged(nameof(HasOverdueTasks));
    }

    public void MarkDirty() => _isDirty = true;

    [RelayCommand]
    private void ToggleSearch()
    {
        if (GlobalSearch.IsVisible)
            GlobalSearch.CloseSearch();
        else
            GlobalSearch.Show();
    }

    [RelayCommand]
    public async Task LoadData()
    {
        IsBusy = true;
        StatusMessage = "Загрузка...";
        try
        {
            var data = await _repo.LoadAsync();
            Tasks.Load(data.Tasks);
            Documents.Load(data.Documents);
            Time.Load(data.TimeEntries);
            _isDirty = false;
            RefreshCounters();
            StatusMessage = $"Загружено: задач {data.Tasks.Count}, " +
                            $"документов {data.Documents.Count}, " +
                            $"записей времени {data.TimeEntries.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SaveData()
    {
        IsBusy = true;
        StatusMessage = "Сохранение...";
        try
        {
            var data = new Models.AppData
            {
                Tasks       = Tasks.Dump().ToList(),
                Documents   = Documents.Dump().ToList(),
                TimeEntries = Time.Dump().ToList()
            };
            await _repo.SaveAsync(data);
            _isDirty = false;
            StatusMessage = $"Сохранено в {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка сохранения: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AutoSaveAsync()
    {
        if (!_isDirty || IsBusy) return;
        try
        {
            var data = new Models.AppData
            {
                Tasks       = Tasks.Dump().ToList(),
                Documents   = Documents.Dump().ToList(),
                TimeEntries = Time.Dump().ToList()
            };
            await _repo.SaveAsync(data);
            _isDirty = false;
            Dispatcher.UIThread.Post(() =>
                StatusMessage = $"Автосохранение в {DateTime.Now:HH:mm:ss}");
        }
        catch { }
    }

    [RelayCommand]
    private async Task ExportTasksCsv()
    {
        var path = await PickSaveFileAsync("Экспорт задач", "tasks_export.csv");
        if (path is null) return;
        CsvExportService.ExportTasks(Tasks.Dump(), path);
        StatusMessage = $"Задачи экспортированы: {Path.GetFileName(path)}";
    }

    [RelayCommand]
    private async Task ExportTimeCsv()
    {
        var path = await PickSaveFileAsync("Экспорт таймшитов", "time_export.csv");
        if (path is null) return;
        CsvExportService.ExportTimeEntries(Time.Dump(), path);
        StatusMessage = $"Таймшиты экспортированы: {Path.GetFileName(path)}";
    }

    [RelayCommand]
    private async Task ExportDocumentsCsv()
    {
        var path = await PickSaveFileAsync("Экспорт документов", "documents_export.csv");
        if (path is null) return;
        CsvExportService.ExportDocuments(Documents.Dump(), path);
        StatusMessage = $"Документы экспортированы: {Path.GetFileName(path)}";
    }

    private static async Task<string?> PickSaveFileAsync(string title, string suggestedName)
    {
        var topLevel = FilePickerHelper.TopLevel;
        if (topLevel is null) return null;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title             = title,
                SuggestedFileName = suggestedName,
                DefaultExtension  = "csv",
                FileTypeChoices   = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("CSV файл")
                    {
                        Patterns = new[] { "*.csv" }
                    }
                }
            });

        return file?.Path.LocalPath;
    }

    public void Dispose() => _autoSaveTimer.Dispose();
}