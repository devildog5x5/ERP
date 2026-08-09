using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ledgerly.Server.Data;
using Ledgerly.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Services;

public static class DatabaseStatusService
{
    // Soft guidance for local SQLite file growth (not a hard engine limit).
    private const long SqliteWatchBytes = 250L * 1024 * 1024;
    private const long SqliteHighBytes = 1024L * 1024 * 1024;
    private const long SqliteCriticalBytes = 2L * 1024 * 1024 * 1024;

    public static DatabaseStatusDto GetStatus()
    {
        var cfg = new ServerConfig { Provider = Db.Provider, ConnectionString = Db.ConnectionString };
        var dto = new DatabaseStatusDto
        {
            Provider = Db.Provider.ToString(),
            ProviderLabel = ProviderLabel(Db.Provider),
            Summary = cfg.Describe(),
            MultiUserReady = Db.IsServerDatabase(Db.Provider),
            Location = ResolveLocation(),
            Characteristics = BuildCharacteristics(Db.Provider)
        };

        using var db = Db.Create();
        dto.Tables = CollectTableStats(db);
        dto.EngineVersion = TryGetEngineVersion(db);

        FillSizeMetrics(dto, db);
        ApplyCapacityLevel(dto);
        dto.Suggestions = BuildSuggestions(dto);
        dto.RecommendedActions = dto.Suggestions
            .Where(s => !string.IsNullOrWhiteSpace(s.ActionKey))
            .Select(s => s.ActionKey!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return dto;
    }

    public static DatabasePurgeResultDto PurgeMaintenance()
    {
        using var db = Db.Create();
        var auditCutoff = DateTime.UtcNow.AddDays(-180);
        var reminderCutoff = DateTime.UtcNow.AddDays(-90);

        var oldAudit = db.AuditLogs.Where(a => a.CreatedAt < auditCutoff).ToList();
        var oldReminders = db.Reminders
            .Where(r => r.IsResolved && r.CreatedAt < reminderCutoff)
            .ToList();

        db.AuditLogs.RemoveRange(oldAudit);
        db.Reminders.RemoveRange(oldReminders);
        db.SaveChanges();

        return new DatabasePurgeResultDto
        {
            AuditLogsRemoved = oldAudit.Count,
            RemindersRemoved = oldReminders.Count,
            Message = $"Removed {oldAudit.Count} audit log(s) older than 180 days and {oldReminders.Count} resolved reminder(s) older than 90 days."
        };
    }

    private static string ProviderLabel(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => "SQL Server",
        DatabaseProvider.MySql => "MySQL / MariaDB",
        DatabaseProvider.PostgreSql => "PostgreSQL",
        _ => "SQLite"
    };

    private static List<string> BuildCharacteristics(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.Sqlite => new List<string>
        {
            "Local single-file database (default for small shops)",
            "Best for one PC or light concurrent use",
            "Backups are simple file copies",
            "When the file or disk grows large, migrate to a server database"
        },
        DatabaseProvider.SqlServer => new List<string>
        {
            "Client/server database for multi-user and larger workloads",
            "Use SQL Server tools (SSMS / BACKUP DATABASE) for backups",
            "Scale with more RAM, CPU, and storage on the SQL host"
        },
        DatabaseProvider.MySql => new List<string>
        {
            "MySQL or MariaDB server for multi-user deployments",
            "Use mysqldump or host backups for recovery",
            "Good fit for Linux or shared hosting environments"
        },
        DatabaseProvider.PostgreSql => new List<string>
        {
            "PostgreSQL server for multi-user and analytics-friendly workloads",
            "Use pg_dump or host backups for recovery",
            "Strong concurrency and reliability characteristics"
        },
        _ => new List<string>()
    };

    private static string? ResolveLocation()
    {
        if (Db.Provider == DatabaseProvider.Sqlite)
        {
            var path = Db.ConnectionString.Replace("Data Source=", "").Trim();
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }
        return new ServerConfig { Provider = Db.Provider, ConnectionString = Db.ConnectionString }.Describe();
    }

    private static List<DatabaseTableStatDto> CollectTableStats(ErpDbContext db)
    {
        var pairs = new (string Name, Func<int> Count)[]
        {
            ("Products", () => db.Products.Count()),
            ("Customers", () => db.Customers.Count()),
            ("Suppliers", () => db.Suppliers.Count()),
            ("SalesOrders", () => db.SalesOrders.Count()),
            ("PurchaseOrders", () => db.PurchaseOrders.Count()),
            ("StockMovements", () => db.StockMovements.Count()),
            ("Reminders", () => db.Reminders.Count()),
            ("AuditLogs", () => db.AuditLogs.Count()),
            ("CrmLeads", () => SafeCount(() => db.CrmLeads.Count())),
            ("CrmAccounts", () => SafeCount(() => db.CrmAccounts.Count())),
            ("CrmOpportunities", () => SafeCount(() => db.CrmOpportunities.Count())),
            ("CrmActivities", () => SafeCount(() => db.CrmActivities.Count())),
            ("Users", () => db.Users.Count())
        };

        return pairs
            .Select(p => new DatabaseTableStatDto { Name = p.Name, Rows = SafeCount(p.Count) })
            .OrderByDescending(t => t.Rows)
            .ToList();
    }

    private static int SafeCount(Func<int> count)
    {
        try { return count(); }
        catch { return 0; }
    }

    private static string? TryGetEngineVersion(ErpDbContext db)
    {
        try
        {
            return Db.Provider switch
            {
                DatabaseProvider.Sqlite => ScalarString(db, "SELECT sqlite_version();"),
                DatabaseProvider.SqlServer => ScalarString(db, "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('ProductVersion'));"),
                DatabaseProvider.MySql => ScalarString(db, "SELECT VERSION();"),
                DatabaseProvider.PostgreSql => ScalarString(db, "SHOW server_version;"),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static string? ScalarString(ErpDbContext db, string sql)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            cmd.Connection.Open();
        cmd.CommandText = sql;
        var result = cmd.ExecuteScalar();
        return result?.ToString();
    }

    private static long? ScalarLong(ErpDbContext db, string sql)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            cmd.Connection.Open();
        cmd.CommandText = sql;
        var result = cmd.ExecuteScalar();
        if (result == null || result == DBNull.Value) return null;
        return Convert.ToInt64(result);
    }

    private static void FillSizeMetrics(DatabaseStatusDto dto, ErpDbContext db)
    {
        try
        {
            if (Db.Provider == DatabaseProvider.Sqlite)
            {
                var path = Db.ConnectionString.Replace("Data Source=", "").Trim();
                if (File.Exists(path))
                {
                    var used = new FileInfo(path).Length;
                    dto.UsedBytes = used;
                    var root = Path.GetPathRoot(Path.GetFullPath(path));
                    if (!string.IsNullOrWhiteSpace(root))
                    {
                        var drive = new DriveInfo(root);
                        dto.FreeBytes = drive.AvailableFreeSpace;
                        dto.CapacityBytes = drive.TotalSize;
                        if (drive.TotalSize > 0)
                            dto.PercentFull = Math.Round(100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize, 1);
                    }
                }
            }
            else if (Db.Provider == DatabaseProvider.SqlServer)
            {
                var used = ScalarLong(db,
                    "SELECT SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS bigint) * 8 * 1024) FROM sys.database_files WHERE type_desc = 'ROWS';");
                var capacity = ScalarLong(db,
                    "SELECT SUM(CAST(size AS bigint) * 8 * 1024) FROM sys.database_files WHERE type_desc = 'ROWS';");
                dto.UsedBytes = used;
                dto.CapacityBytes = capacity;
                if (used.HasValue && capacity.HasValue && capacity.Value > 0)
                {
                    dto.FreeBytes = Math.Max(0, capacity.Value - used.Value);
                    dto.PercentFull = Math.Round(100.0 * used.Value / capacity.Value, 1);
                }
            }
            else if (Db.Provider == DatabaseProvider.MySql)
            {
                var used = ScalarLong(db,
                    "SELECT COALESCE(SUM(data_length + index_length), 0) FROM information_schema.tables WHERE table_schema = DATABASE();");
                dto.UsedBytes = used;
            }
            else if (Db.Provider == DatabaseProvider.PostgreSql)
            {
                var used = ScalarLong(db, "SELECT pg_database_size(current_database());");
                dto.UsedBytes = used;
            }
        }
        catch
        {
            // Best-effort metrics; UI still shows provider + table counts.
        }

        dto.UsedDisplay = FormatBytes(dto.UsedBytes);
        dto.FreeDisplay = FormatBytes(dto.FreeBytes);
        dto.CapacityDisplay = FormatBytes(dto.CapacityBytes);
        dto.PercentDisplay = dto.PercentFull.HasValue ? $"{dto.PercentFull:0.#}%" : "n/a";
    }

    private static void ApplyCapacityLevel(DatabaseStatusDto dto)
    {
        var level = "ok";
        var label = "Healthy";

        if (Db.Provider == DatabaseProvider.Sqlite && dto.UsedBytes.HasValue)
        {
            if (dto.UsedBytes >= SqliteCriticalBytes) { level = "critical"; label = "Critical — SQLite file is very large"; }
            else if (dto.UsedBytes >= SqliteHighBytes) { level = "high"; label = "High — consider migrating soon"; }
            else if (dto.UsedBytes >= SqliteWatchBytes) { level = "watch"; label = "Watch — growing local database"; }
        }

        if (dto.PercentFull.HasValue)
        {
            if (dto.PercentFull >= 95) { level = "critical"; label = "Critical — storage nearly full"; }
            else if (dto.PercentFull >= 85 && Rank(level) < Rank("high")) { level = "high"; label = "High — free up space soon"; }
            else if (dto.PercentFull >= 70 && Rank(level) < Rank("watch")) { level = "watch"; label = "Watch — disk usage climbing"; }
        }

        if (dto.FreeBytes.HasValue && dto.FreeBytes < 2L * 1024 * 1024 * 1024 && Rank(level) < Rank("high"))
        {
            level = "high";
            label = "High — less than 2 GB free";
        }
        if (dto.FreeBytes.HasValue && dto.FreeBytes < 500L * 1024 * 1024)
        {
            level = "critical";
            label = "Critical — less than 500 MB free";
        }

        dto.CapacityLevel = level;
        dto.CapacityLabel = label;
    }

    private static int Rank(string level) => level switch
    {
        "critical" => 3,
        "high" => 2,
        "watch" => 1,
        _ => 0
    };

    private static List<DatabaseSuggestionDto> BuildSuggestions(DatabaseStatusDto dto)
    {
        var list = new List<DatabaseSuggestionDto>();
        var audit = dto.Tables.FirstOrDefault(t => t.Name == "AuditLogs")?.Rows ?? 0;
        var reminders = dto.Tables.FirstOrDefault(t => t.Name == "Reminders")?.Rows ?? 0;
        var movements = dto.Tables.FirstOrDefault(t => t.Name == "StockMovements")?.Rows ?? 0;

        list.Add(new DatabaseSuggestionDto
        {
            Severity = "info",
            Title = "Keep a recent backup",
            Detail = Db.Provider == DatabaseProvider.Sqlite
                ? "Use Integrations → Backup now (copies the .db file) before migrations or cleanup."
                : "Use your database host backup tools regularly. Coalesce can write a reminder note under Backups.",
            ActionKey = "backup"
        });

        if (Db.Provider == DatabaseProvider.Sqlite)
        {
            list.Add(new DatabaseSuggestionDto
            {
                Severity = dto.CapacityLevel is "high" or "critical" ? dto.CapacityLevel : "info",
                Title = "Migrate when you outgrow SQLite",
                Detail = "Stop the server and run: Coalesce.Server.exe migrate --provider SqlServer|MySql|PostgreSql --connection \"...\". The client stays the same.",
                ActionKey = "migrate"
            });
        }

        if (dto.CapacityLevel is "watch" or "high" or "critical")
        {
            list.Add(new DatabaseSuggestionDto
            {
                Severity = dto.CapacityLevel,
                Title = "Free disk space on the server PC",
                Detail = "Empty recycle bin, move large downloads/media off the system drive, and keep at least several GB free for growth and backups.",
                ActionKey = "free-disk"
            });
        }

        if (audit > 5000 || reminders > 2000 || dto.CapacityLevel is "high" or "critical")
        {
            list.Add(new DatabaseSuggestionDto
            {
                Severity = audit > 20000 || reminders > 10000 ? "high" : "watch",
                Title = "Purge old maintenance data",
                Detail = "Remove audit logs older than 180 days and resolved reminders older than 90 days. Business records (orders, inventory, CRM) are kept.",
                ActionKey = "purge"
            });
        }

        if (movements > 100000)
        {
            list.Add(new DatabaseSuggestionDto
            {
                Severity = "watch",
                Title = "Stock history is large",
                Detail = "High stock-movement volume is normal for busy warehouses. Prefer a server database (SQL Server / MySQL / PostgreSQL) for smoother performance.",
                ActionKey = Db.Provider == DatabaseProvider.Sqlite ? "migrate" : null
            });
        }

        if (dto.CapacityLevel == "critical")
        {
            list.Add(new DatabaseSuggestionDto
            {
                Severity = "critical",
                Title = "Act before the database cannot grow",
                Detail = "Back up first, free disk space, purge old logs if needed, then migrate off SQLite or expand the server database storage.",
                ActionKey = "backup"
            });
        }

        if (list.Count == 1)
        {
            list.Add(new DatabaseSuggestionDto
            {
                Severity = "info",
                Title = "No urgent capacity action",
                Detail = "Usage looks fine. Recheck this dialog periodically after busy seasons or large imports."
            });
        }

        return list;
    }

    private static string FormatBytes(long? bytes)
    {
        if (!bytes.HasValue) return "n/a";
        double v = bytes.Value;
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        var i = 0;
        while (v >= 1024 && i < units.Length - 1)
        {
            v /= 1024;
            i++;
        }
        return $"{v:0.##} {units[i]}";
    }
}
