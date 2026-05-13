using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PilotApp.Models;

namespace PilotApp.Services;

public class AuditService
{
    private readonly string _path;
    private readonly List<AuditLogEntry> _entries = new();

    public AuditService(string path)
    {
        _path = path;
    }

    public async Task LoadAsync()
    {
        if (!File.Exists(_path)) return;
        var json = await File.ReadAllTextAsync(_path);
        var loaded = JsonSerializer.Deserialize<List<AuditLogEntry>>(json);
        if (loaded != null)
        {
            _entries.Clear();
            _entries.AddRange(loaded);
        }
    }

    public IReadOnlyList<AuditLogEntry> GetAll() => _entries;

    public void Log(string login, AuditEventType eventType, string details = "", bool success = true)
    {
        _entries.Add(new AuditLogEntry
        {
            Login = login,
            EventType = eventType,
            Details = details,
            Success = success
        });

        _ = FlushAsync();
    }

    private async Task FlushAsync()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_path, json);
        }
        catch { }
    }
}