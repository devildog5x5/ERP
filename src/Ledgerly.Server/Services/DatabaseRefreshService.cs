using System;
using System.IO;
using System.Threading;
using Ledgerly.Server.Data;
using Ledgerly.Shared;

namespace Ledgerly.Server.Services;

public static class DatabaseRefreshService
{
    public const string RequiredConfirmation = "REFRESH DATABASE";

    public static DatabaseRefreshResultDto Refresh()
    {
        // Safety copy first (SQLite file backup; SQL Server writes a note).
        var backup = BackupService.Backup();

        // EnsureDeleted drops the store for both SQLite and SQL Server.
        using (var db = Db.Create())
        {
            db.Database.EnsureDeleted();
        }

        // SQLite may leave -wal/-shm briefly locked; clean them up if present.
        if (Db.Provider == DatabaseProvider.Sqlite)
            TryDeleteSqliteSidecars();

        DbSeeder.Seed();

        return new DatabaseRefreshResultDto
        {
            Refreshed = true,
            BackupPath = backup.Path,
            Message = "Database wiped and reseeded. Sign in again as admin / admin."
        };
    }

    private static void TryDeleteSqliteSidecars()
    {
        var path = Db.ConnectionString.Replace("Data Source=", "").Trim();
        if (string.IsNullOrWhiteSpace(path)) return;

        foreach (var candidate in new[] { path + "-wal", path + "-shm" })
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    if (File.Exists(candidate))
                        File.Delete(candidate);
                    break;
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
