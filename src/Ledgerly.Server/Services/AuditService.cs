using Ledgerly.Server.Data;

namespace Ledgerly.Server.Services;

public static class AuditService
{
    public static void Write(ErpDbContext db, int? userId, string userName, string action, string entityType, int? entityId, string? details = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserName = userName ?? "",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        });
    }
}
