using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IRepository _repo;
    private readonly AuditService _audit;
    private readonly Timer _autoSaveTimer;
    private readonly BackupService _backup;
    private bool _isDirty;

    private UserAccount CurrentUser { get; }

    public TasksViewModel Tasks { get; } = new();
    public DocumentsViewModel Documents { get; } = new();
    public TimeViewModel Time { get; } = new();
    public KanbanViewModel Kanban { get; }
    public GlobalSearchViewModel GlobalSearch { get; }
    public DashboardViewModel Dashboard { get; }
    public AuditLogViewModel AuditLog { get; }
    public UsersViewModel? Users { get; }

    [ObservableProperty] private string _statusMessage = "Готово";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isDarkTheme = true;

    partial void OnIsDarkThemeChanged(bool value) => ApplyTheme(value);

    private static void ApplyTheme(bool dark)
    {
        if (Application.Current is not App app) return;
        app.RequestedThemeVariant = dark
            ? Avalonia.Styling.ThemeVariant.Dark
            : Avalonia.Styling.ThemeVariant.Light;
    }

    public bool CanCreate => PermissionService.CanCreateTask(CurrentUser.Role);
    public bool CanDelete => PermissionService.CanDeleteTask(CurrentUser.Role);
    public bool CanEdit => PermissionService.CanEditTask(CurrentUser.Role);
    public bool CanViewAudit => PermissionService.CanViewAuditLog(CurrentUser.Role);

    public string TasksHeader => Tasks.Tasks.Count > 0 ? $"Задачи ({Tasks.Tasks.Count})" : "Задачи";
    public string DocumentsHeader => Documents.Documents.Count > 0 ? $"Документы ({Documents.Documents.Count})" : "Документы";
    public string TimeHeader => Time.Entries.Count > 0 ? $"Время ({Time.Entries.Count})" : "Время";
    public string KanbanHeader => "Канбан";
    public string DashboardHeader => "Обзор";
    public string AuditHeader => "Журнал";
    public string UsersHeader => "Пользователи";
    public string CurrentUserInfo => $"{CurrentUser.DisplayName} ({CurrentUser.Role})";

    public MainWindowViewModel(IRepository repo, AuditService audit, UserAccount currentUser, UserRepository userRepo, BackupService backup)
    {
        _repo = repo;
        _audit = audit;
        _backup = backup;
        CurrentUser = currentUser;

        Kanban = new KanbanViewModel(Tasks);
        GlobalSearch = new GlobalSearchViewModel(Tasks, Documents, Time);
        Dashboard = new DashboardViewModel(Tasks, Documents, Time);
        AuditLog = new AuditLogViewModel(audit);

        if (PermissionService.CanManageUsers(currentUser.Role))
            Users = new UsersViewModel(userRepo, audit, currentUser.Login);

        Time.SetTasksSource(Tasks);
        Documents.SetTasksSource(Tasks);

        Tasks.SetDirtyCallback(MarkDirty);
        Documents.SetDirtyCallback(MarkDirty);
        Time.SetDirtyCallback(MarkDirty);

        Tasks.SetPermissions(CurrentUser.Role, _audit, CurrentUser.Login);
        Documents.SetPermissions(CurrentUser.Role, _audit, CurrentUser.Login);
        Time.SetPermissions(CurrentUser.Role, _audit, CurrentUser.Login);

        Kanban.SetPermissions(CurrentUser.Role, CurrentUser.DisplayName);
        Dashboard.SetPermissions(CurrentUser.Role, CurrentUser.DisplayName);

        var dialog = new DialogService();
        Tasks.SetDialogService(dialog);
        Documents.SetDialogService(dialog);
        Time.SetDialogService(dialog);

        Tasks.Tasks.CollectionChanged += (_, _) => RefreshCounters();
        Documents.Documents.CollectionChanged += (_, _) => RefreshCounters();
        Time.Entries.CollectionChanged += (_, _) => RefreshCounters();

        _autoSaveTimer = new Timer(
            _ => { _ = AutoSaveAsync(); },
            null,
            TimeSpan.FromMinutes(3),
            TimeSpan.FromMinutes(3));
    }

    private void RefreshCounters()
    {
        OnPropertyChanged(nameof(TasksHeader));
        OnPropertyChanged(nameof(DocumentsHeader));
        OnPropertyChanged(nameof(TimeHeader));
    }

    private void MarkDirty() => _isDirty = true;

    [RelayCommand]
    private void ToggleSearch()
    {
        if (GlobalSearch.IsVisible) GlobalSearch.CloseSearch();
        else GlobalSearch.Show();
    }

    [RelayCommand]
    private async Task LoadData()
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
            StatusMessage = $"Загружено: задач {data.Tasks.Count}, документов {data.Documents.Count}, записей времени {data.TimeEntries.Count}";
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
    private async Task SaveData()
    {
        IsBusy = true;
        StatusMessage = "Сохранение...";
        try
        {
            var data = new AppData
            {
                Tasks = Tasks.Dump().ToList(),
                Documents = Documents.Dump().ToList(),
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
            var data = new AppData
            {
                Tasks = Tasks.Dump().ToList(),
                Documents = Documents.Dump().ToList(),
                TimeEntries = Time.Dump().ToList()
            };
            await _repo.SaveAsync(data);
            _isDirty = false;
            Dispatcher.UIThread.Post(() => StatusMessage = $"Автосохранение в {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception)
        {
        }
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
                Title = title,
                SuggestedFileName = suggestedName,
                DefaultExtension = "csv",
                FileTypeChoices = [new Avalonia.Platform.Storage.FilePickerFileType("CSV файл") { Patterns = ["*.csv"] }]
            });
        return file?.Path.LocalPath;
    }

    [RelayCommand]
    private async Task Logout()
    {
        _audit.Log(CurrentUser.Login, AuditEventType.Logout, "Выход из системы");
        await SaveData();

        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime) return;

        var loginWindow = new PilotApp.Views.LoginWindow(
            ((App)Application.Current!).UserRepo,
            _audit);

        var oldWindow = lifetime.MainWindow;
        loginWindow.Show();
        oldWindow?.Close();

        await loginWindow.WaitForLoginAsync();

        if (loginWindow.LoggedInUser == null)
        {
            lifetime.Shutdown();
            return;
        }

        var mainVm = new MainWindowViewModel(
            _repo, _audit, loginWindow.LoggedInUser,
            ((App)Application.Current!).UserRepo,
            ((App)Application.Current!).Backup);

        var mainWindow = new PilotApp.Views.MainWindow { DataContext = mainVm };
        lifetime.MainWindow = mainWindow;
        mainWindow.Show();
        _ = mainVm.LoadDataCommand.ExecuteAsync(null);
    }
    [RelayCommand]
    private async Task CreateBackup()
    {
        IsBusy = true;
        StatusMessage = "Создание резервной копии...";
        try
        {
            await SaveData();
            await _backup.CreateAllBackupsAsync();
            StatusMessage = $"Резервная копия создана в {DateTime.Now:HH:mm:ss}";
            _audit.Log(CurrentUser.Login, AuditEventType.LoginSuccess, "Создана резервная копия данных");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ошибка резервного копирования: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Dispose() => _autoSaveTimer.Dispose();
}