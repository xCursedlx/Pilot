using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IRepository _repo;

    public TasksViewModel     Tasks     { get; } = new();
    public DocumentsViewModel Documents { get; } = new();
    public TimeViewModel      Time      { get; } = new();

    [ObservableProperty]
    private string statusMessage = "Готово";

    [ObservableProperty]
    private bool isBusy;

    public MainWindowViewModel(IRepository repo)
    {
        _repo = repo;
    }

    [RelayCommand]
    private async Task LoadData()
    {
        IsBusy = true;
        StatusMessage = "Загрузка…";
        try
        {
            var data = await _repo.LoadAsync();
            Tasks.Load(data.Tasks);
            Documents.Load(data.Documents);
            Time.Load(data.TimeEntries);
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
    private async Task SaveData()
    {
        IsBusy = true;
        StatusMessage = "Сохранение…";
        try
        {
            var data = new Models.AppData
            {
                Tasks       = Tasks.Dump().ToList(),
                Documents   = Documents.Dump().ToList(),
                TimeEntries = Time.Dump().ToList()
            };
            await _repo.SaveAsync(data);
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
}
