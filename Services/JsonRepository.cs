using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PilotApp.Models;

namespace PilotApp.Services;

public sealed class JsonRepository : IRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonRepository(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }

    public async Task<AppData> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
                return new AppData();

            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<AppData>(stream, Options, ct)
                   ?? new AppData();
        }
        catch
        {
            return new AppData();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(AppData data, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var tmp = _filePath + ".tmp";
            await using (var stream = File.Create(tmp))
                await JsonSerializer.SerializeAsync(stream, data, Options, ct);

            File.Copy(tmp, _filePath, overwrite: true);
            File.Delete(tmp);
        }
        finally
        {
            _lock.Release();
        }
    }
}