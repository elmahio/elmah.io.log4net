using Elmah.Io.AspNetCore.Log4Net;

namespace Microsoft.AspNetCore.Builder
{
    public static class ElmahIoAspNetCoreLog4NetExtensions
    {
        public static IApplicationBuilder UseElmahIoLog4Net(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ElmahIoLog4NetMiddleware>();
        }
    }
}
