using System;
using System.IO;
using System.Linq;
using Ledgerly.Server.Data;

namespace Ledgerly.Server.Services;

public static class BackupService
{
    public static string BackupDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Coalesce", "Backups");

    public static Shared.BackupResultDto Backup()
    {
        Directory.CreateDirectory(BackupDirectory);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        if (Db.Provider == DatabaseProvider.Sqlite)
        {
            var src = Db.ConnectionString.Replace("Data Source=", "").Trim();
            if (!File.Exists(src)) throw new FileNotFoundException("SQLite database not found", src);
            var dest = Path.Combine(BackupDirectory, $"coalesce-{stamp}.db");
            File.Copy(src, dest, overwrite: true);
            return new Shared.BackupResultDto { Path = dest, CreatedAt = DateTime.Now, Provider = "Sqlite" };
        }

        var note = Path.Combine(BackupDirectory, $"{Db.Provider.ToString().ToLowerInvariant()}-backup-{stamp}.txt");
        var safe = new ServerConfig { Provider = Db.Provider, ConnectionString = Db.ConnectionString }.Describe();
        var toolHint = Db.Provider switch
        {
            DatabaseProvider.MySql =>
                "Use mysqldump or your host's backup tool, e.g.\r\n" +
                $"mysqldump -u USER -p DATABASE > coalesce-{stamp}.sql",
            DatabaseProvider.PostgreSql =>
                "Use pg_dump or your host's backup tool, e.g.\r\n" +
                $"pg_dump -U USER DATABASE > coalesce-{stamp}.sql",
            _ =>
                "Use SQL Server Management Studio or:\r\n" +
                $"BACKUP DATABASE [YourDb] TO DISK = 'C:\\Backup\\Coalesce-{stamp}.bak'"
        };
        File.WriteAllText(note,
            $"{Db.Provider} provider is active.\r\n" +
            $"{toolHint}\r\n" +
            $"Target : {safe}\r\n");
        return new Shared.BackupResultDto { Path = note, CreatedAt = DateTime.Now, Provider = Db.Provider.ToString() };
    }

    public static void RestoreSqlite(string backupPath)
    {
        if (Db.Provider != DatabaseProvider.Sqlite)
            throw new InvalidOperationException(
                "File restore is only supported for SQLite. Use your database vendor's restore tools for SQL Server, MySQL, or PostgreSQL.");

        var resolved = ResolveBackupPath(backupPath);
        if (!File.Exists(resolved))
            throw new FileNotFoundException("Backup not found", resolved);
        if (!string.Equals(Path.GetExtension(resolved), ".db", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only .db backups from the Coalesce backup folder can be restored.");

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
            throw new InvalidOperationException("Restore path must be inside the Coalesce backup folder.");

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
