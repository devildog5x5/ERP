using System;
using System.Linq;
using Ledgerly.Shared;

namespace Ledgerly.Client.Services;

public static class Session
{
    public static LoginResponseDto? Current { get; set; }
    public static bool IsLoggedIn => Current != null && !string.IsNullOrWhiteSpace(Current.Token);
    public static string DisplayName => Current?.DisplayName ?? "Guest";
    public static string Role => Current?.Role ?? "";

    /// <summary>Only Administrators may manage users and assign access levels.</summary>
    public static bool IsAdministrator =>
        Current != null &&
        (string.Equals(Current.Role, "Administrator", StringComparison.OrdinalIgnoreCase) ||
         PermissionSet().Contains("admin"));

    public static bool Can(string permission)
    {
        if (Current == null || string.IsNullOrWhiteSpace(permission)) return false;

        var want = permission.Trim().ToLowerInvariant();
        // Access-level / user administration is Administrator-only (not grantable via role CSV).
        if (want is "users" or "admin")
            return IsAdministrator;

        if (IsAdministrator) return true;
        return PermissionSet().Contains(want);
    }

    private static System.Collections.Generic.HashSet<string> PermissionSet() =>
        (Current?.Permissions ?? "")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .Where(p => p.Length > 0)
            .ToHashSet();

    /// <summary>True if the user has any of the comma-separated permissions.</summary>
    public static bool CanAny(string permissionsCsv)
    {
        if (string.IsNullOrWhiteSpace(permissionsCsv)) return true;
        return permissionsCsv
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Any(p => Can(p.Trim()));
    }

    public static void Clear() => Current = null;
}
