using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PilotApp.Services;

public class BackupService
{
    private readonly string _dataDir;
    private readonly int _maxBackups;

    public BackupService(string dataDir, int maxBackups = 7)
    {
        _dataDir = dataDir;
        _maxBackups = maxBackups;
    }

    public async Task CreateBackupAsync(string fileName)
    {
        var sourcePath = Path.Combine(_dataDir, fileName);
        if (!File.Exists(sourcePath)) return;

        var backupDir = Path.Combine(_dataDir, "backups");
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";
        var backupPath = Path.Combine(backupDir, backupName);

        await Task.Run(() => File.Copy(sourcePath, backupPath, overwrite: true));

        await RotateBackupsAsync(backupDir, fileName);
    }

    public async Task CreateAllBackupsAsync()
    {
        await CreateBackupAsync("data.json");
        await CreateBackupAsync("users.json");
        await CreateBackupAsync("audit.json");
    }

    public BackupInfo[] GetBackups(string fileName)
    {
        var backupDir = Path.Combine(_dataDir, "backups");
        if (!Directory.Exists(backupDir)) return [];

        var prefix = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);

        return Directory.GetFiles(backupDir, $"{prefix}_*{ext}")
            .Select(f => new BackupInfo(
                Path.GetFileName(f),
                f,
                File.GetCreationTime(f),
                new FileInfo(f).Length))
            .OrderByDescending(b => b.CreatedAt)
            .ToArray();
    }

    public async Task RestoreBackupAsync(string backupPath)
    {
        if (!File.Exists(backupPath)) return;

        var fileName = ExtractOriginalFileName(Path.GetFileName(backupPath));
        var targetPath = Path.Combine(_dataDir, fileName);

        await CreateBackupAsync(fileName);
        await Task.Run(() => File.Copy(backupPath, targetPath, overwrite: true));
    }

    private async Task RotateBackupsAsync(string backupDir, string fileName)
    {
        var backups = GetBackups(fileName);
        if (backups.Length <= _maxBackups) return;

        var toDelete = backups.Skip(_maxBackups).ToArray();
        await Task.Run(() =>
        {
            foreach (var b in toDelete)
                File.Delete(b.FullPath);
        });
    }

    private static string ExtractOriginalFileName(string backupFileName)
    {
        var ext = Path.GetExtension(backupFileName);
        var name = Path.GetFileNameWithoutExtension(backupFileName);
        var lastUnderscore = name.LastIndexOf('_');
        if (lastUnderscore < 0) return backupFileName;
        var withoutTime = name[..lastUnderscore];
        var secondUnderscore = withoutTime.LastIndexOf('_');
        if (secondUnderscore < 0) return backupFileName;
        return withoutTime[..secondUnderscore] + ext;
    }
}

public sealed record BackupInfo(string FileName, string FullPath, DateTime CreatedAt, long SizeBytes);