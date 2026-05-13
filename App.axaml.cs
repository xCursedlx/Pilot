using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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
    public BackupService Backup { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PilotApp");

            Directory.CreateDirectory(appDataPath);

            var encryptionKey = GetMachineKey(appDataPath);
            Backup = new BackupService(appDataPath);

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

            var repo = new JsonRepository(
                Path.Combine(appDataPath, "data.json"),
                encryptionKey);

            var mainVm = new MainWindowViewModel(repo, audit, loginWindow.LoggedInUser, UserRepo, Backup);
            var mainWindow = new MainWindow { DataContext = mainVm };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            await mainVm.LoadDataCommand.ExecuteAsync(null);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string GetMachineKey(string appDataPath)
    {
        var keyFile = Path.Combine(appDataPath, ".key");

        if (File.Exists(keyFile))
            return File.ReadAllText(keyFile);

        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        File.WriteAllText(keyFile, key);

        var fi = new FileInfo(keyFile);
        fi.Attributes |= FileAttributes.Hidden;

        return key;
    }
}