using System;
using System.IO;
using Avalonia.Controls;
using PilotApp.Services;
using PilotApp.ViewModels;

namespace PilotApp.Views;

public partial class MainWindow : Window
{
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        FilePickerHelper.TopLevel = TopLevel.GetTopLevel(this);

        var repo = new JsonRepository(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PilotApp", "data.json"));

        var vm = new MainWindowViewModel(repo);
        DataContext = vm;
        _ = vm.LoadDataCommand.ExecuteAsync(null);

        vm.StatusMessage = $"Готово  |  бэкапы: {repo.BackupDirectory}";

        Closing += async (_, e) =>
        {
            if (_isClosing) return;
            e.Cancel    = true;
            _isClosing  = true;
            try
            {
                await vm.SaveDataCommand.ExecuteAsync(null);
            }
            finally
            {
                vm.Dispose();
                Close();
            }
        };
    }
}