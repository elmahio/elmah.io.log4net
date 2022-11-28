using Elmah.Io.Log4Net.Console70;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddTransient<Service>();
    })
    .ConfigureLogging(logBuilder =>
    {
        logBuilder.SetMinimumLevel(LogLevel.Trace);
        logBuilder.AddLog4Net("log4net.config");

    }).UseConsoleLifetime();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<Service>();
    service.Execute();
}