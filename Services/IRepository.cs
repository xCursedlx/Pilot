using System.Threading;
using System.Threading.Tasks;
using PilotApp.Models;

namespace PilotApp.Services;

public interface IRepository
{
    Task<AppData> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(AppData data, CancellationToken ct = default);
}