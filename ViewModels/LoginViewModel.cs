using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly UserRepository _userRepo;
    private readonly AuditService _audit;

    public System.Action<UserAccount>? OnLoginSuccess { get; set; }

    [ObservableProperty] private string login = string.Empty;
    [ObservableProperty] private string password = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isBusy;

    public LoginViewModel(UserRepository userRepo, AuditService audit)
    {
        _userRepo = userRepo;
        _audit = audit;
    }

    [RelayCommand]
    private async Task SignIn()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Login) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Введите логин и пароль.";
            return;
        }

        IsBusy = true;

        await Task.Delay(300);

        var user = _userRepo.FindByLogin(Login.Trim());

        if (user == null || !PasswordService.Verify(Password, user.PasswordHash))
        {
            _audit.Log(Login.Trim(), AuditEventType.LoginFailed,
                $"Неудачная попытка входа с логином '{Login.Trim()}'", success: false);
            ErrorMessage = "Неверный логин или пароль.";
            IsBusy = false;
            return;
        }

        if (!user.IsActive)
        {
            _audit.Log(Login.Trim(), AuditEventType.LoginFailed,
                "Учётная запись заблокирована.", success: false);
            ErrorMessage = "Учётная запись заблокирована.";
            IsBusy = false;
            return;
        }

        await _userRepo.UpdateLastLoginAsync(user);
        _audit.Log(user.Login, AuditEventType.LoginSuccess, $"Вход выполнен. Роль: {user.Role}");

        IsBusy = false;
        OnLoginSuccess?.Invoke(user);
    }
}