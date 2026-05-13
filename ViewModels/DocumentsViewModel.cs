using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class DocumentsViewModel : ObservableObject
{
    private Action? _markDirty;
    private IDialogService? _dialog;
    private TasksViewModel? _tasksVm;
    private UserRole _role = UserRole.Admin;
    private AuditService? _audit;
    private string _currentLogin = string.Empty;

    public bool CanCreate => PermissionService.CanCreateDocument(_role);
    public bool CanDelete => PermissionService.CanDeleteDocument(_role);
    public bool CanEdit => PermissionService.CanEditDocument(_role);

    public void SetDirtyCallback(Action markDirty) => _markDirty = markDirty;
    public void SetDialogService(IDialogService dialog) => _dialog = dialog;

    public void SetPermissions(UserRole role, AuditService audit, string login)
    {
        _role = role;
        _audit = audit;
        _currentLogin = login;
        OnPropertyChanged(nameof(CanCreate));
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanEdit));
    }

    public void SetTasksSource(TasksViewModel tasks)
    {
        _tasksVm = tasks;
        _tasksVm.Tasks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(AvailableTasks));
    }

    public IEnumerable<TaskItem> AvailableTasks =>
        _tasksVm?.Tasks ?? Enumerable.Empty<TaskItem>();

    [ObservableProperty] private TaskItem? selectedLinkedTask;

    partial void OnSelectedLinkedTaskChanged(TaskItem? value)
    {
        if (SelectedDocument is null) return;
        SelectedDocument.LinkedTaskId = value?.Id.ToString();
        _markDirty?.Invoke();
    }

    public ObservableCollection<DocumentItem> Documents { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteDocumentCommand))]
    private DocumentItem? selectedDocument;

    partial void OnSelectedDocumentChanged(DocumentItem? value)
    {
        if (value is null) { SelectedLinkedTask = null; return; }
        if (Guid.TryParse(value.LinkedTaskId, out var id))
            SelectedLinkedTask = _tasksVm?.Tasks.FirstOrDefault(t => t.Id == id);
        else
            SelectedLinkedTask = null;
    }

    [ObservableProperty] private string filterText = string.Empty;

    public IEnumerable<DocumentItem> FilteredDocuments =>
        Documents.Where(d =>
            string.IsNullOrWhiteSpace(FilterText) ||
            d.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
            (d.Version ?? "").Contains(FilterText, StringComparison.OrdinalIgnoreCase));

    [RelayCommand]
    private void AddDocument()
    {
        var d = new DocumentItem
        {
            Name = "Новый документ",
            Version = GetNextVersion(),
            CreatedAt = DateTime.Now
        };
        Documents.Add(d);
        SelectedDocument = d;
        _markDirty?.Invoke();
        _audit?.Log(_currentLogin, AuditEventType.DocumentCreated, $"Создан документ: {d.Name}");
        OnFilterChanged();
    }

    private string GetNextVersion()
    {
        if (Documents.Count == 0) return "v1";
        var siblings = SelectedDocument is not null
            ? Documents.Where(d => d.Name == SelectedDocument.Name).ToList()
            : Documents.ToList();
        var maxNum = siblings
            .Select(d => d.Version)
            .Select(v => int.TryParse(v.TrimStart('v'), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"v{maxNum + 1}";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteDoc))]
    private async Task DeleteDocument()
    {
        if (SelectedDocument is null) return;
        if (_dialog is not null)
        {
            var ok = await _dialog.ConfirmAsync("Удаление документа", $"Удалить документ «{SelectedDocument.Name}»?");
            if (!ok) return;
        }
        var title = SelectedDocument.Name;
        var idx = Documents.IndexOf(SelectedDocument);
        Documents.Remove(SelectedDocument);
        SelectedDocument = Documents.Count == 0 ? null : Documents[Math.Clamp(idx, 0, Documents.Count - 1)];
        _markDirty?.Invoke();
        _audit?.Log(_currentLogin, AuditEventType.DocumentDeleted, $"Удалён документ: {title}");
        OnFilterChanged();
    }

    private bool CanDeleteDoc() => SelectedDocument is not null && CanDelete;

    [RelayCommand]
    private async Task PickFile()
    {
        if (SelectedDocument is null) return;
        var dialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
        {
            Title = "Выберите файл документа",
            AllowMultiple = false
        };
        var topLevel = FilePickerHelper.TopLevel;
        if (topLevel is null) return;
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(dialog);
        if (files.Count == 0) return;
        SelectedDocument.FilePath = files[0].Path.LocalPath;
        _markDirty?.Invoke();
        OnPropertyChanged(nameof(SelectedDocument));
        OnFilterChanged();
    }

    partial void OnFilterTextChanged(string value) => OnFilterChanged();
    private void OnFilterChanged() => OnPropertyChanged(nameof(FilteredDocuments));

    public void Load(IEnumerable<DocumentItem> items)
    {
        Documents.Clear();
        foreach (var d in items) Documents.Add(d);
        SelectedDocument = Documents.FirstOrDefault();
        OnFilterChanged();
    }

    public IEnumerable<DocumentItem> Dump() => Documents;
}