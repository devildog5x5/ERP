using System;
using System.Text;
using Ledgerly.Server.Data;
using Ledgerly.Shared;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Services;

public static class DatabaseGrowService
{
    public const string RequiredConfirmation = "GROW DATABASE";

    public static DatabaseConnectionTestResultDto TestConnection(DatabaseConnectionTestDto dto)
    {
        if (!TryResolve(dto.Provider, dto.Host, dto.Port, dto.Database, dto.Username, dto.Password,
                dto.UseWindowsAuth, dto.ConnectionString, out var provider, out var cs, out var error))
            return new DatabaseConnectionTestResultDto { Ok = false, Message = error };

        try
        {
            using var db = Db.Create(provider, cs);
            if (!db.Database.CanConnect())
                return new DatabaseConnectionTestResultDto
                {
                    Ok = false,
                    Message = "Could not connect. Check host, database name, and credentials."
                };

            db.Database.EnsureCreated();
            var summary = new ServerConfig { Provider = provider, ConnectionString = cs }.Describe();
            return new DatabaseConnectionTestResultDto
            {
                Ok = true,
                Provider = provider.ToString(),
                Summary = summary,
                Message = "Connection OK. Schema is ready (or already exists)."
            };
        }
        catch (Exception ex)
        {
            return new DatabaseConnectionTestResultDto
            {
                Ok = false,
                Message = ex.InnerException?.Message ?? ex.Message
            };
        }
    }

    public static DatabaseGrowResultDto Grow(DatabaseGrowDto dto)
    {
        if (!string.Equals((dto.Confirmation ?? "").Trim(), RequiredConfirmation, StringComparison.Ordinal))
            throw new InvalidOperationException($"Type {RequiredConfirmation} to confirm.");

        if (!TryResolve(dto.Provider, dto.Host, dto.Port, dto.Database, dto.Username, dto.Password,
                dto.UseWindowsAuth, dto.ConnectionString, out var provider, out var cs, out var error))
            throw new InvalidOperationException(error);

        if (provider == DatabaseProvider.Sqlite)
            throw new InvalidOperationException("Choose SQL Server, MySQL, or PostgreSQL to grow.");

        if (provider == Db.Provider &&
            string.Equals(cs, Db.ConnectionString.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Already using this database.");

        var mode = (dto.Mode ?? "CopyAndSwitch").Trim();
        var backup = BackupService.Backup();

        MigrationResult result;
        if (string.Equals(mode, "EmptyAndSwitch", StringComparison.OrdinalIgnoreCase))
            result = PlatformMigrator.PointToEmpty(provider, cs, switchConfig: true);
        else
            result = PlatformMigrator.MigrateTo(provider, cs, switchConfig: true);

        var summary = new ServerConfig { Provider = provider, ConnectionString = cs }.Describe();
        return new DatabaseGrowResultDto
        {
            Success = true,
            Provider = provider.ToString(),
            Summary = summary,
            BackupPath = backup.Path,
            ConfigUpdated = result.ConfigUpdated,
            Counts = result.Counts,
            Message = string.Equals(mode, "EmptyAndSwitch", StringComparison.OrdinalIgnoreCase)
                ? $"Now using {provider}. Target was empty (system tables created). Sign in again if prompted."
                : $"Copied your data to {provider} and switched the server. Backup: {backup.Path}"
        };
    }

    public static bool TryResolve(
        string? providerName,
        string? host,
        int? port,
        string? database,
        string? username,
        string? password,
        bool useWindowsAuth,
        string? connectionStringOverride,
        out DatabaseProvider provider,
        out string connectionString,
        out string error)
    {
        provider = DatabaseProvider.SqlServer;
        connectionString = "";
        error = "";

        if (!ServerConfig.TryParseProvider(providerName, out provider) || provider == DatabaseProvider.Sqlite)
        {
            error = "Provider must be SqlServer, MySql, or PostgreSql.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(connectionStringOverride))
        {
            connectionString = connectionStringOverride.Trim();
            return true;
        }

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(database))
        {
            error = "Host and database name are required (or provide a full connection string).";
            return false;
        }

        connectionString = BuildConnectionString(provider, host!.Trim(), port, database!.Trim(),
            username?.Trim(), password, useWindowsAuth);
        return true;
    }

    public static string BuildConnectionString(
        DatabaseProvider provider,
        string host,
        int? port,
        string database,
        string? username,
        string? password,
        bool useWindowsAuth)
    {
        return provider switch
        {
            DatabaseProvider.MySql => BuildMySql(host, port ?? 3306, database, username, password),
            DatabaseProvider.PostgreSql => BuildPostgres(host, port ?? 5432, database, username, password),
            _ => BuildSqlServer(host, port, database, username, password, useWindowsAuth)
        };
    }

    private static string BuildSqlServer(
        string host, int? port, string database, string? username, string? password, bool useWindowsAuth)
    {
        var server = port is > 0 ? $"{host},{port}" : host;
        var sb = new StringBuilder();
        sb.Append("Server=").Append(server).Append(';');
        sb.Append("Database=").Append(database).Append(';');
        if (useWindowsAuth || string.IsNullOrWhiteSpace(username))
        {
            sb.Append("Trusted_Connection=True;");
        }
        else
        {
            sb.Append("User Id=").Append(username).Append(';');
            sb.Append("Password=").Append(password ?? "").Append(';');
        }
        sb.Append("TrustServerCertificate=True;");
        return sb.ToString();
    }

    private static string BuildMySql(string host, int port, string database, string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("MySQL requires a username.");
        return $"Server={host};Port={port};Database={database};User={username};Password={password ?? ""};";
    }

    private static string BuildPostgres(string host, int port, string database, string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("PostgreSQL requires a username.");
        return $"Host={host};Port={port};Database={database};Username={username};Password={password ?? ""};";
    }
}
