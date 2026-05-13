using System.Threading.Tasks;
using Avalonia.Controls;
using PilotApp.Models;
using PilotApp.Services;
using PilotApp.ViewModels;

namespace PilotApp.Views;

public partial class LoginWindow : Window
{
    private readonly TaskCompletionSource _tcs = new();

    public UserAccount? LoggedInUser { get; private set; }

    public LoginWindow(UserRepository userRepo, AuditService audit)
    {
        InitializeComponent();
        var vm = new LoginViewModel(userRepo, audit);
        vm.OnLoginSuccess = user =>
        {
            LoggedInUser = user;
            _tcs.TrySetResult();
            Close();
        };
        Closed += (_, _) => _tcs.TrySetResult();
        DataContext = vm;
    }

    public Task WaitForLoginAsync() => _tcs.Task;
}