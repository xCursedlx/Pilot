using System;
using System.IO;
using Avalonia.Controls;
using PilotApp.Services;
using PilotApp.ViewModels;

namespace PilotApp.Views;

public partial class MainWindow : Window
{
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
        Closing += async (_, e) =>
        {
            e.Cancel = true;
            await vm.SaveDataCommand.ExecuteAsync(null);
            Closing -= null;
            Close();
        };
    }
}