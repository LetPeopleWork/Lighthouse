namespace Lighthouse.Backend.Configuration
{
    public class RateLimitingConfiguration
    {
        public const string SectionName = "RateLimits";

        public const string AuthLoginPolicy = "AuthLogin";
        public const string ApiKeysPolicy = "ApiKeys";
        public const string BootstrapSystemAdminPolicy = "BootstrapSystemAdmin";

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
