using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace MoxfieldPriceScraper;

public static class LoggerFactory
{
    /// <summary>
    /// Creates a logger with the specified logging level
    /// </summary>
    /// <param name="isDebug">Denotes if we are in debug mode and require more thorough logging.</param>
    /// <returns>A logger with the required logging level.</returns>
    public static ILogger CreateLogger(bool isDebug)
    {
        var logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        // Create logging directory if it doesn't exist
        Directory.CreateDirectory(logsPath);

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(GetLoggingLevel(isDebug))
            .WriteTo.Console()
            .WriteTo.File(new JsonFormatter(),
                $"{Path.Combine(logsPath, "errors.json")}",
                restrictedToMinimumLevel: LogEventLevel.Error)
            .WriteTo.File($"{Path.Combine(logsPath, "all-.log")}",
                rollingInterval: RollingInterval.Month);

        return loggerConfiguration.CreateLogger();
    }

    /// <summary>
    /// Returns a LoggingLevelSwitch object with the specified logging level.
    /// </summary>
    /// <param name="isDebug">Denotes if debug logging is required.</param>
    /// <returns>The LoggingLevelSwitch containing the logging required logging level.</returns>
    private static LoggingLevelSwitch GetLoggingLevel(bool isDebug)
    {
        return new LoggingLevelSwitch()
        {
            MinimumLevel = isDebug ? LogEventLevel.Debug : LogEventLevel.Information
        };
    }
}
