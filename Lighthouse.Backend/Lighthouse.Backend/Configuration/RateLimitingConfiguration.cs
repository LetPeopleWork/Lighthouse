namespace Lighthouse.Backend.Configuration
{
    public class RateLimitingConfiguration
    {
        public const string SectionName = "RateLimits";

        public const string AuthLoginPolicy = "AuthLogin";
        public const string ApiKeysPolicy = "ApiKeys";
        public const string BootstrapSystemAdminPolicy = "BootstrapSystemAdmin";
        public const string EmbedSessionPolicy = "EmbedSession";

        // Unauthenticated, and every call writes a durable row. Left open it is an insertion sink -
        // and worse, one caller could plant a granted row and keep an instance sending for a whole
        // liveness window regardless of what any real person chose.
        public const string UsageDataConsentPolicy = "UsageDataConsent";

        public bool Enabled { get; set; } = true;

        public Dictionary<string, FixedWindowPolicyConfiguration> Policies { get; set; } = new();
    }

    public class FixedWindowPolicyConfiguration
    {
        public int PermitLimit { get; set; }

        public int WindowSeconds { get; set; }

        public int QueueLimit { get; set; }
    }
}
