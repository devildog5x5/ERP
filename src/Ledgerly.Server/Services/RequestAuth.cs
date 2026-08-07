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

    public static bool HasPermission(AppUser? user, string permission)
    {
        if (user?.Role == null) return false;
        var perms = (user.Role.Permissions ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim().ToLowerInvariant()).ToList();
        return perms.Contains("admin") || perms.Contains(permission.ToLowerInvariant()) ||
               user.Role.Name.Equals("Administrator", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAnonymousAllowed(HttpActionContext actionContext)
    {
        var path = actionContext.Request.RequestUri?.AbsolutePath?.ToLowerInvariant() ?? "";
        return path.EndsWith("/api/health") ||
               path.EndsWith("/api/auth/login") ||
               path.Contains("/api/auth/login");
    }
}
