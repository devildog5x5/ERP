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
        if (settings != null && !settings.RequireLogin)
            return;

        var user = RequestAuth.GetUser(actionContext.Request);
        if (user is null)
        {
            actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized,
                "Login required. POST /api/auth/login with userName/password (default admin / admin).");
            return;
        }

        actionContext.Request.Properties["LedgerlyUser"] = user;
        base.OnActionExecuting(actionContext);
    }
}
