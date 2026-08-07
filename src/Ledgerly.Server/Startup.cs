using System.Net.Http.Formatting;
using System.Web.Http;
using Ledgerly.Server.Services;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;

[assembly: OwinStartup(typeof(Ledgerly.Server.Startup))]

namespace Ledgerly.Server;

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var config = new HttpConfiguration();
        config.MapHttpAttributeRoutes();
        config.Filters.Add(new AuthFilter());

        config.Formatters.Clear();
        config.Formatters.Add(new JsonMediaTypeFormatter
        {
            SerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None
            }
        });

        app.UseCors(CorsOptions.AllowAll);
        app.UseWebApi(config);
    }
}
