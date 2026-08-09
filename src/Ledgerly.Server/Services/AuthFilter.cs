using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Ledgerly.Server.Data;

namespace Ledgerly.Server.Services;

public sealed class AuthFilter : ActionFilterAttribute
{
    public override void OnActionExecuting(HttpActionContext actionContext)
    {
        if (RequestAuth.IsAnonymousAllowed(actionContext))
            return;

        using var db = Db.Create();
        var settings = db.Settings.FirstOrDefault();
        var requireLogin = settings == null || settings.RequireLogin;

        var user = RequestAuth.GetUser(actionContext.Request);
        var required = actionContext.ActionDescriptor.GetCustomAttributes<RequirePermissionAttribute>()
            .Concat(actionContext.ActionDescriptor.ControllerDescriptor
                .GetCustomAttributes<RequirePermissionAttribute>())
            .Select(a => a.Permission)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .ToList();

        if (required.Count > 0)
        {
            if (user is null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized,
                    "Login required.");
                return;
            }

            foreach (var perm in required)
            {
                if (!RequestAuth.HasPermission(user, perm))
                {
                    actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden,
                        $"Missing permission: {perm}");
                    return;
                }
            }
        }
        else if (requireLogin)
        {
            if (user is null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized,
                    "Login required. POST /api/auth/login with userName and password.");
                return;
            }
        }

        if (user != null)
            actionContext.Request.Properties["LedgerlyUser"] = user;

        base.OnActionExecuting(actionContext);
    }
}
