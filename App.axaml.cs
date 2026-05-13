using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PilotApp.Services;
using PilotApp.Views;
using PilotApp.ViewModels;

namespace PilotApp;

public partial class App : Application
{
    public UserRepository UserRepo { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PilotApp");

            UserRepo = new UserRepository(Path.Combine(appDataPath, "users.json"));
            var audit = new AuditService(Path.Combine(appDataPath, "audit.json"));

            await UserRepo.LoadAsync();
            await audit.LoadAsync();

            var loginWindow = new LoginWindow(UserRepo, audit);
            loginWindow.Show();
            await loginWindow.WaitForLoginAsync();

            if (loginWindow.LoggedInUser == null)
            {
                desktop.Shutdown();
                return;
            }

            var repo = new JsonRepository(Path.Combine(appDataPath, "data.json"));
            var mainVm = new MainWindowViewModel(repo, audit, loginWindow.LoggedInUser, UserRepo);
            var mainWindow = new MainWindow { DataContext = mainVm };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            await mainVm.LoadDataCommand.ExecuteAsync(null);
        }

        base.OnFrameworkInitializationCompleted();
    }
}