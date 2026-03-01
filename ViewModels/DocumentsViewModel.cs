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

public partial class DocumentsViewModel : ObservableObject
{
    public ObservableCollection<DocumentItem> Documents { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteDocumentCommand))]
    private DocumentItem? selectedDocument;

    [ObservableProperty]
    private string filterText = string.Empty;

    public IEnumerable<DocumentItem> FilteredDocuments =>
        Documents.Where(d =>
            string.IsNullOrWhiteSpace(FilterText) ||
            d.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
            (d.Version ?? "").Contains(FilterText, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<string> Versions { get; } =
        new[] { "v1", "v2", "v3", "v4", "v5" };

    [RelayCommand]
    private void AddDocument()
    {
        var d = new DocumentItem
        {
            Name = "Новый документ",
            Version = "v1",
            CreatedAt = DateTime.Now
        };
        Documents.Add(d);
        SelectedDocument = d;
        OnFilterChanged();
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void DeleteDocument()
    {
        if (SelectedDocument is null) return;
        var idx = Documents.IndexOf(SelectedDocument);
        Documents.Remove(SelectedDocument);
        SelectedDocument = Documents.Count == 0
            ? null
            : Documents[Math.Clamp(idx, 0, Documents.Count - 1)];
        OnFilterChanged();
    }

    private bool CanDelete() => SelectedDocument is not null;

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
        OnPropertyChanged(nameof(SelectedDocument));
        OnFilterChanged();
    }


    partial void OnFilterTextChanged(string value) => OnFilterChanged();

    private void OnFilterChanged() =>
        OnPropertyChanged(nameof(FilteredDocuments));

    public void Load(IEnumerable<DocumentItem> items)
    {
        Documents.Clear();
        foreach (var d in items)
            Documents.Add(d);
        SelectedDocument = Documents.FirstOrDefault();
        OnFilterChanged();
    }

    public IEnumerable<DocumentItem> Dump() => Documents;
}
