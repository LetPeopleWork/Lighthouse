using Lighthouse.Backend.Configuration;
using Serilog;
using Serilog.Core;
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

        public static Logger CreateLogger(IConfiguration configuration, LoggingLevelSwitch levelSwitch)
        {
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
                .CreateLogger();
        }
    }
}
