using log4net;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Elmah.Io.AspNetCore.Log4Net
{
    /// <summary>
    /// Middleware class for ASP.NET Core that enrich all log messages with the HTTP context.
    /// </summary>
    /// <remarks>
    /// Create a new instance of the middleware. You typically don't want to call this manually.
    /// </remarks>
    public class ElmahIoLog4NetMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        /// <summary>
        /// Invoke the middleware. You typically don't want to call this manually.
        /// </summary>
        public async Task Invoke(HttpContext context)
        {
            LogicalThreadContext.Properties["url"] = context.Request?.Path.Value;
            LogicalThreadContext.Properties["method"] = context.Request?.Method;
            LogicalThreadContext.Properties["statuscode"] = context.Response.StatusCode;
            LogicalThreadContext.Properties["user"] = context.User?.Identity?.Name;
            LogicalThreadContext.Properties["servervariables"] = ServerVariables(context);
            LogicalThreadContext.Properties["cookies"] = Cookies(context);
            LogicalThreadContext.Properties["form"] = Form(context);
            LogicalThreadContext.Properties["querystring"] = QueryString(context);

            await _next.Invoke(context);
        }

        private static Dictionary<string, string> QueryString(HttpContext context)
        {
            return context.Request?.Query?.Keys.ToDictionary(k => k, k => context.Request.Query[k].ToString());
        }

        private static Dictionary<string, string> Form(HttpContext context)
        {
            try
            {
                return context.Request?.Form?.Keys.ToDictionary(k => k, k => context.Request.Form[k].ToString());
            }
            catch (InvalidOperationException)
            {
                // Request not a form POST or similar
            }

            return [];
        }

        private static Dictionary<string, string> Cookies(HttpContext context)
        {
            return context.Request?.Cookies?.Keys.ToDictionary(k => k, k => context.Request.Cookies[k].ToString());
        }

        private static Dictionary<string, string> ServerVariables(HttpContext context)
        {
            return context.Request?.Headers?.Keys.ToDictionary(k => k, k => context.Request.Headers[k].ToString());
        }
    }
}
