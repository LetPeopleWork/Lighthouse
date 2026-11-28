namespace Lighthouse.Backend.Configuration
{
    public class ResponseCompressionConfiguration
    {
        public const string SectionName = "ResponseCompression";

        public bool Enabled { get; set; } = true;
    }
}
