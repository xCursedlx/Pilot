using System.Threading.Tasks;

namespace PilotApp.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message);
}