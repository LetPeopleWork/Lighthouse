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
        public string Format { get; set; } = string.Empty;

        public bool IsJson => string.Equals(Format, "json", StringComparison.OrdinalIgnoreCase);
    }
}
