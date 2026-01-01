namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors
{
    public static class AuthenticationMethodKeys
    {
        // Azure DevOps
        public const string AzureDevOpsPat = "ado.pat";

        // Jira
        public const string JiraCloud = "jira.cloud";
        
        public const string JiraDataCenter = "jira.datacenter";

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
