namespace Lighthouse.Backend.Configuration
{
    public class ShutdownConfiguration
    {
        public const string SectionName = "Shutdown";

        public int TimeoutSeconds { get; set; } = 30;
    }
}
