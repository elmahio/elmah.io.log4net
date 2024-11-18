using Elmah.Io.AspNetCore.Log4Net;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.AspNetCore.Builder
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Extension methods to help install the elmah.io log4net middleware in ASP.NET Core.
    /// </summary>
    public static class ElmahIoAspNetCoreLog4NetExtensions
    {
        /// <summary>
        /// Install the elmah.io log4net middleware for ASP.NET Core that enrich all log messages
        /// with HTTP contextual information like cookies and server variables.
        /// </summary>
        public static IApplicationBuilder UseElmahIoLog4Net(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ElmahIoLog4NetMiddleware>();
        }
    }
}
