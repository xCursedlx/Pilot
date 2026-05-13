using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using PilotApp.Models;

namespace PilotApp.Services;

public class UserRepository
{
    private readonly string _path;
    private List<UserAccount> _users = new();

    public UserRepository(string path)
    {
        _path = path;
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_path))
        {
            await SeedDefaultUsersAsync();
            return;
        }

        var json = await File.ReadAllTextAsync(_path);
        _users = JsonSerializer.Deserialize<List<UserAccount>>(json) ?? new();
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_path, json);
    }

    public UserAccount? FindByLogin(string login)
    {
        return _users.FirstOrDefault(u =>
            u.Login.Equals(login, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<UserAccount> GetAll() => _users;

    public void Add(UserAccount user) => _users.Add(user);

    public void Remove(UserAccount user) => _users.Remove(user);

    public async Task UpdateLastLoginAsync(UserAccount user)
    {
        user.LastLogin = DateTime.Now;
        await SaveAsync();
    }

    private async Task SeedDefaultUsersAsync()
    {
        _users = new List<UserAccount>
        {
            new UserAccount
            {
                Login = "admin",
                PasswordHash = PasswordService.Hash("admin123"),
                Role = UserRole.Admin,
                DisplayName = "Администратор"
            },
            new UserAccount
            {
                Login = "manager",
                PasswordHash = PasswordService.Hash("manager123"),
                Role = UserRole.Manager,
                DisplayName = "Менеджер"
            },
            new UserAccount
            {
                Login = "user",
                PasswordHash = PasswordService.Hash("user123"),
                Role = UserRole.User,
                DisplayName = "Пользователь"
            }
        };

        await SaveAsync();
    }
}