namespace Lighthouse.Backend.Configuration
{
    public class EmbedConfiguration
    {
        public const string SectionName = "Embed";

        public int TokenLifetimeSeconds { get; set; } = 60;

        public int SessionLifetimeMinutes { get; set; } = 30;
    }
}
