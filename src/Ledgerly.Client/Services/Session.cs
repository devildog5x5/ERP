using Ledgerly.Shared;

namespace Ledgerly.Client.Services;

public static class Session
{
    public static LoginResponseDto? Current { get; set; }
    public static bool IsLoggedIn => Current != null && !string.IsNullOrWhiteSpace(Current.Token);
    public static string DisplayName => Current?.DisplayName ?? "Guest";
    public static string Role => Current?.Role ?? "";

    public static bool Can(string permission)
    {
        if (Current == null) return false;
        if (string.Equals(Current.Role, "Administrator", System.StringComparison.OrdinalIgnoreCase)) return true;
        var perms = (Current.Permissions ?? "").ToLowerInvariant();
        return perms.Contains(permission.ToLowerInvariant());
    }

    public static void Clear() => Current = null;
}
