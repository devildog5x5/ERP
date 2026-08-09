using System;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using Ledgerly.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Server.Services;

public static class RequestAuth
{
    public static AppUser? GetUser(HttpRequestMessage request)
    {
        if (!request.Headers.TryGetValues("Authorization", out var values))
            return null;
        var header = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header)) return null;
        var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header.Substring(7).Trim()
            : header.Trim();
        if (string.IsNullOrWhiteSpace(token)) return null;

        using var db = Db.Create();
        var row = db.AuthTokens.Include(t => t.User).ThenInclude(u => u.Role)
            .FirstOrDefault(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);
        if (row?.User == null || !row.User.IsActive) return null;
        return row.User;
    }

    public static bool IsAdministrator(AppUser? user)
    {
        if (user?.Role == null) return false;
        if (user.Role.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
            return true;
        return PermissionSet(user).Contains("admin");
    }

    public static bool HasPermission(AppUser? user, string permission)
    {
        if (user?.Role == null || string.IsNullOrWhiteSpace(permission)) return false;

        var want = permission.Trim().ToLowerInvariant();
        // User management / assigning access levels is Administrator-only.
        if (want is "users" or "admin")
            return IsAdministrator(user);

        if (IsAdministrator(user)) return true;
        return PermissionSet(user).Contains(want);
    }

    private static System.Collections.Generic.HashSet<string> PermissionSet(AppUser user) =>
        (user.Role.Permissions ?? "")
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant())
            .Where(p => p.Length > 0)
            .ToHashSet();

    public static bool IsAnonymousAllowed(HttpActionContext actionContext)
    {
        var path = (actionContext.Request.RequestUri?.AbsolutePath ?? "").TrimEnd('/').ToLowerInvariant();
        return path.EndsWith("/api/health") || path.EndsWith("/api/auth/login");
    }
}
