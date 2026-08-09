using System;
using System.IO;
using System.Linq;
using Ledgerly.Server.Data;

namespace Ledgerly.Server.Services;

public static class BackupService
{
    public static string BackupDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ledgerly", "Backups");

    public static Shared.BackupResultDto Backup()
    {
        Directory.CreateDirectory(BackupDirectory);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        if (Db.Provider == DatabaseProvider.Sqlite)
        {
            var src = Db.ConnectionString.Replace("Data Source=", "").Trim();
            if (!File.Exists(src)) throw new FileNotFoundException("SQLite database not found", src);
            var dest = Path.Combine(BackupDirectory, $"ledgerly-{stamp}.db");
            File.Copy(src, dest, overwrite: true);
            return new Shared.BackupResultDto { Path = dest, CreatedAt = DateTime.Now, Provider = "Sqlite" };
        }

        // SQL Server: write a restore instruction file (never persist the raw connection string)
        var note = Path.Combine(BackupDirectory, $"sqlserver-backup-{stamp}.txt");
        var safe = new ServerConfig { Provider = DatabaseProvider.SqlServer, ConnectionString = Db.ConnectionString }.Describe();
        File.WriteAllText(note,
            "SQL Server provider is active.\r\n" +
            "Use SQL Server Management Studio or:\r\n" +
            $"BACKUP DATABASE [YourDb] TO DISK = 'C:\\Backup\\Ledgerly-{stamp}.bak'\r\n" +
            $"Target : {safe}\r\n");
        return new Shared.BackupResultDto { Path = note, CreatedAt = DateTime.Now, Provider = "SqlServer" };
    }

    public static void RestoreSqlite(string backupPath)
    {
        if (Db.Provider != DatabaseProvider.Sqlite)
            throw new InvalidOperationException("File restore is only supported for SQLite. Use SQL Server restore tools for SQL Server.");

        var resolved = ResolveBackupPath(backupPath);
        if (!File.Exists(resolved))
            throw new FileNotFoundException("Backup not found", resolved);
        if (!string.Equals(Path.GetExtension(resolved), ".db", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only .db backups from the Ledgerly backup folder can be restored.");

        var dest = Db.ConnectionString.Replace("Data Source=", "").Trim();
        File.Copy(resolved, dest, overwrite: true);
    }

    /// <summary>
    /// Accepts either a file name or a full path, but only files under BackupDirectory.
    /// </summary>
    public static string ResolveBackupPath(string backupPath)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
            throw new ArgumentException("Backup path required.", nameof(backupPath));

        Directory.CreateDirectory(BackupDirectory);
        var root = Path.GetFullPath(BackupDirectory);

        string candidate;
        if (Path.IsPathRooted(backupPath))
            candidate = Path.GetFullPath(backupPath);
        else
            candidate = Path.GetFullPath(Path.Combine(BackupDirectory, backupPath));

        if (!candidate.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(candidate, root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Restore path must be inside the Ledgerly backup folder.");

        if (candidate.Contains(".."))
            throw new InvalidOperationException("Invalid backup path.");

        return candidate;
    }

    public static bool IsKnownBackup(string path)
    {
        try
        {
            var resolved = ResolveBackupPath(path);
            return File.Exists(resolved) &&
                   Directory.GetFiles(BackupDirectory).Any(f =>
                       string.Equals(Path.GetFullPath(f), resolved, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }
}
