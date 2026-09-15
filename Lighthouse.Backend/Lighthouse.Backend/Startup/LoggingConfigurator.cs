using Lighthouse.Backend.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Settings.Configuration;
using Serilog.Templates;

namespace Lighthouse.Backend.Startup
{
    public static class LoggingConfigurator
    {
        private const string ConsoleTextTemplate =
            "{@t:HH:mm:ss} - {@l:u} - {Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}: {@m}\n{@x}";

        private const string ConsoleJsonTemplate =
            "{ {Timestamp: @t, Level: @l, SourceContext: SourceContext, Message: @m, Exception: @x, ..@p} }\n";

        /// <summary>
        /// <paramref name="recentProblems"/> is a peer of the file sink, taking the same events at
        /// <see cref="LogEventLevel.Warning"/> and above. It arrives as an instance for the same reason
        /// <paramref name="levelSwitch"/> does: the logger is built before dependency injection exists, so
        /// anything the rest of the application must also reach has to be constructed by the caller and
        /// registered by it.
        /// </summary>
        public static Logger CreateLogger(IConfiguration configuration, LoggingLevelSwitch levelSwitch, ILogEventSink recentProblems)
        {
            ArgumentNullException.ThrowIfNull(recentProblems);

            var telemetryConfig = configuration
                .GetSection(TelemetryConfiguration.SectionName)
                .Get<TelemetryConfiguration>() ?? new TelemetryConfiguration();

            var consoleTemplate = telemetryConfig.Logging.IsJson ? ConsoleJsonTemplate : ConsoleTextTemplate;

            var readerOptions = new ConfigurationReaderOptions(
                typeof(FileLoggerConfigurationExtensions).Assembly,
                typeof(ConsoleLoggerConfigurationExtensions).Assembly,
                typeof(ExpressionTemplate).Assembly);

            return new LoggerConfiguration()
                .ReadFrom.Configuration(configuration, readerOptions)
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext()
                .WriteTo.Console(new ExpressionTemplate(consoleTemplate))
                .WriteTo.Sink(recentProblems, LogEventLevel.Warning)
                .CreateLogger();
        }
    }
}
