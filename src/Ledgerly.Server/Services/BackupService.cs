using System;
using System.IO;
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

        // SQL Server: write a restore instruction file + optional BACPAC hint
        var note = Path.Combine(BackupDirectory, $"sqlserver-backup-{stamp}.txt");
        File.WriteAllText(note,
            "SQL Server provider is active.\r\n" +
            "Use SQL Server Management Studio or:\r\n" +
            $"BACKUP DATABASE [YourDb] TO DISK = 'C:\\Backup\\Ledgerly-{stamp}.bak'\r\n" +
            $"Connection: {Db.ConnectionString}\r\n");
        return new Shared.BackupResultDto { Path = note, CreatedAt = DateTime.Now, Provider = "SqlServer" };
    }

    public static void RestoreSqlite(string backupPath)
    {
        if (Db.Provider != DatabaseProvider.Sqlite)
            throw new InvalidOperationException("File restore is only supported for SQLite. Use SQL Server restore tools for SQL Server.");
        if (!File.Exists(backupPath)) throw new FileNotFoundException("Backup not found", backupPath);
        var dest = Db.ConnectionString.Replace("Data Source=", "").Trim();
        File.Copy(backupPath, dest, overwrite: true);
    }
}
