namespace Lighthouse.Backend.Configuration
{
    public class TelemetryConfiguration
    {
        public const string SectionName = "Telemetry";

        public bool Enabled { get; set; }

        public TelemetryLoggingConfiguration Logging { get; set; } = new();
    }

    public class TelemetryLoggingConfiguration
    {
        public const string JsonFormat = "json";

        public string Format { get; set; } = string.Empty;

        public bool IsJson => string.Equals(Format, JsonFormat, StringComparison.OrdinalIgnoreCase);
    }
}
