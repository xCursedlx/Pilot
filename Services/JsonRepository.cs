using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PilotApp.Models;

namespace PilotApp.Services;

public sealed class JsonRepository : IRepository
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly string _backupDir;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const int MaxBackups = 7;

    public string BackupDirectory => _backupDir;

    public JsonRepository(string filePath)
    {
        _filePath  = filePath;
        _backupDir = Path.Combine(Path.GetDirectoryName(_filePath)!, "backups");

        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        Directory.CreateDirectory(_backupDir);
    }

    public async Task<AppData> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (File.Exists(_filePath))
            {
                var result = await TryReadAsync(_filePath, ct);
                if (result is not null) return result;
            }

            var lastBackup = GetBackupFiles().FirstOrDefault();
            if (lastBackup is not null)
            {
                var result = await TryReadAsync(lastBackup, ct);
                if (result is not null) return result;
            }

            return new AppData();
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<AppData?> TryReadAsync(string path, CancellationToken ct)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<AppData>(stream, Options, ct);
        }
        catch (JsonException)
        {
            return null;
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

            if (File.Exists(_filePath))
                CreateBackup();

            File.Copy(tmp, _filePath, overwrite: true);
            File.Delete(tmp);

            RotateBackups();
        }
        finally
        {
            _lock.Release();
        }
    }

    private void CreateBackup()
    {
        var stamp      = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupPath = Path.Combine(_backupDir, $"data_{stamp}.json");
        File.Copy(_filePath, backupPath, overwrite: false);
    }

    private void RotateBackups()
    {
        var files = GetBackupFiles();
        foreach (var old in files.Skip(MaxBackups))
        {
            try { File.Delete(old); }
            catch { }
        }
    }

    private string[] GetBackupFiles() =>
        Directory.GetFiles(_backupDir, "data_*.json")
                 .OrderByDescending(f => f)
                 .ToArray();
}