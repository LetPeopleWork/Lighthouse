namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors
{
    public static class AuthenticationMethodKeys
    {
        // Azure DevOps
        public const string AzureDevOpsPat = "ado.pat";

        public const string AzureDevOpsOAuth = "ado.oauth";

        // Jira
        public const string JiraCloud = "jira.cloud";

        public const string JiraDataCenter = "jira.datacenter";

        public const string JiraScopedToken = "jira.scopedtoken";

        public const string JiraOAuth = "jira.oauth";

        // Test-only OAuth provider — only registered when Lighthouse:OAuth:UseStubProvider=true.
        public const string StubOAuth = "stub.oauth";

        // Linear
        public const string LinearApiKey = "linear.apikey";

        // No authentication (e.g., CSV)
        public const string None = "none";

        public static string GetDefaultForSystem(WorkTrackingSystems system)
        {
            return system switch
            {
                WorkTrackingSystems.AzureDevOps => AzureDevOpsPat,
                WorkTrackingSystems.Jira => JiraCloud,
                WorkTrackingSystems.Linear => LinearApiKey,
                WorkTrackingSystems.Csv => None,
                _ => throw new NotSupportedException($"No default authentication method for {system}")
            };
        }
    }
}
