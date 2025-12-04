using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostBuilder()
    .ConfigureLogging(logBuilder =>
    {
        logBuilder.SetMinimumLevel(LogLevel.Trace);
        logBuilder.AddLog4Net("log4net.config");

    }).UseConsoleLifetime();

var host = builder.Build();

using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    var i = 0;
    var result = 42 / i;
    if (logger.IsEnabled(LogLevel.Information))
    {
        logger.LogInformation("Result is {Result}", result);
    }
}
catch (Exception e)
{
    logger.LogError(e, "Error during Execute");
}