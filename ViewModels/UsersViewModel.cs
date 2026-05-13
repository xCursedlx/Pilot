using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PilotApp.Models;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class UsersViewModel : ObservableObject
{
    private readonly UserRepository _userRepo;
    private readonly AuditService _audit;
    private readonly string _currentLogin;

    public ObservableCollection<UserAccount> Users { get; } = new();
    public IReadOnlyList<UserRole> Roles { get; } = System.Enum.GetValues<UserRole>();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteUserCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveUserCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleActiveCommand))]
    private UserAccount? selectedUser;

    partial void OnSelectedUserChanged(UserAccount? value)
    {
        EditDisplayName = value?.DisplayName ?? string.Empty;
        EditRole = value?.Role ?? UserRole.User;
        EditPassword = string.Empty;
        ErrorMessage = string.Empty;
    }

    [ObservableProperty] private string newLogin = string.Empty;
    [ObservableProperty] private string newPassword = string.Empty;
    [ObservableProperty] private string newDisplayName = string.Empty;
    [ObservableProperty] private UserRole newRole = UserRole.User;

    [ObservableProperty] private string editDisplayName = string.Empty;
    [ObservableProperty] private UserRole editRole = UserRole.User;
    [ObservableProperty] private string editPassword = string.Empty;

    [ObservableProperty] private string errorMessage = string.Empty;

    public UsersViewModel(UserRepository userRepo, AuditService audit, string currentLogin)
    {
        _userRepo = userRepo;
        _audit = audit;
        _currentLogin = currentLogin;
        Reload();
    }

    private void Reload()
    {
        var selectedLogin = SelectedUser?.Login;
        Users.Clear();
        foreach (var u in _userRepo.GetAll()) Users.Add(u);
        SelectedUser = selectedLogin != null ? Users.FirstOrDefault(u => u.Login == selectedLogin) : null;
    }

    [RelayCommand]
    private async Task AddUser()
    {
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewLogin))
        {
            ErrorMessage = "Введите логин.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Введите пароль.";
            return;
        }
        if (NewPassword.Length < 6)
        {
            ErrorMessage = "Пароль должен содержать минимум 6 символов.";
            return;
        }
        if (_userRepo.FindByLogin(NewLogin.Trim()) != null)
        {
            ErrorMessage = "Пользователь с таким логином уже существует.";
            return;
        }

        var user = new UserAccount
        {
            Login = NewLogin.Trim(),
            PasswordHash = PasswordService.Hash(NewPassword),
            DisplayName = string.IsNullOrWhiteSpace(NewDisplayName) ? NewLogin.Trim() : NewDisplayName.Trim(),
            Role = NewRole,
            IsActive = true
        };

        _userRepo.Add(user);
        await _userRepo.SaveAsync();
        _audit.Log(_currentLogin, AuditEventType.UserCreated, $"Создан пользователь: {user.Login} ({user.Role})");

        NewLogin = string.Empty;
        NewPassword = string.Empty;
        NewDisplayName = string.Empty;
        NewRole = UserRole.User;
        Reload();
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task SaveUser()
    {
        if (SelectedUser is null) return;
        ErrorMessage = string.Empty;

        if (!string.IsNullOrWhiteSpace(EditPassword) && EditPassword.Length < 6)
        {
            ErrorMessage = "Новый пароль должен содержать минимум 6 символов.";
            return;
        }

        SelectedUser.DisplayName = string.IsNullOrWhiteSpace(EditDisplayName)
            ? SelectedUser.Login
            : EditDisplayName.Trim();
        SelectedUser.Role = EditRole;

        if (!string.IsNullOrWhiteSpace(EditPassword))
            SelectedUser.PasswordHash = PasswordService.Hash(EditPassword);

        await _userRepo.SaveAsync();
        _audit.Log(_currentLogin, AuditEventType.UserCreated, $"Изменён пользователь: {SelectedUser.Login}");
        Reload();
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task DeleteUser()
    {
        if (SelectedUser is null) return;
        if (SelectedUser.Login == _currentLogin)
        {
            ErrorMessage = "Нельзя удалить текущего пользователя.";
            return;
        }

        var login = SelectedUser.Login;
        _userRepo.Remove(SelectedUser);
        await _userRepo.SaveAsync();
        _audit.Log(_currentLogin, AuditEventType.UserDeleted, $"Удалён пользователь: {login}");
        Reload();
    }

    [RelayCommand(CanExecute = nameof(CanModify))]
    private async Task ToggleActive()
    {
        if (SelectedUser is null) return;
        if (SelectedUser.Login == _currentLogin)
        {
            ErrorMessage = "Нельзя заблокировать текущего пользователя.";
            return;
        }
        SelectedUser.IsActive = !SelectedUser.IsActive;
        await _userRepo.SaveAsync();
        Reload();
    }

    private bool CanModify() => SelectedUser is not null;
}