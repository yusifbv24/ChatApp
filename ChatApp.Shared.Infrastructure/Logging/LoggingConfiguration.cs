using Serilog;
using Serilog.Events;

namespace ChatApp.Shared.Infrastructure.Logging
{
    public static class LoggingConfiguration
    {
        public static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application","ChatApp")
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();
        }
    }
}