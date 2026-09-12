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

        // Anonymous write surface on a product that is often reachable from the internet. Left open,
        // one caller can spend the whole allowance the maintainer pays for - and this one is counted
        // per browser rather than per address, because fifty colleagues behind one office address
        // would otherwise throttle each other and silence the instances worth hearing from.
        public const string UsageDataIngestPolicy = "UsageDataIngest";

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
