namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors
{
    /// <summary>
    /// Represents an authentication method with its display name and required options.
    /// </summary>
    public class AuthenticationMethod
    {
        public required string Key { get; init; }
        public required string DisplayName { get; init; }
        public required List<AuthenticationMethodOption> Options { get; init; }
    }

    /// <summary>
    /// Represents an option required by an authentication method.
    /// </summary>
    public class AuthenticationMethodOption
    {
        public required string Key { get; init; }
        public required string DisplayName { get; init; }
        public required bool IsSecret { get; init; }
        public bool IsOptional { get; init; } = false;
    }

    /// <summary>
    /// Provides the authentication schema for each work tracking system.
    /// This is the single source of truth for auth methods and their required options.
    /// </summary>
    public static class AuthenticationMethodSchema
    {
        private static readonly Dictionary<WorkTrackingSystems, List<AuthenticationMethod>> MethodsBySystem = new()
        {
            {
                WorkTrackingSystems.AzureDevOps,
                [
                    new AuthenticationMethod
                    {
                        Key = AuthenticationMethodKeys.AzureDevOpsPat,
                        DisplayName = "Personal Access Token",
                        Options =
                        [
                            new AuthenticationMethodOption { Key = AzureDevOps.AzureDevOpsWorkTrackingOptionNames.Url, DisplayName = "Organization URL", IsSecret = false },
                            new AuthenticationMethodOption { Key = AzureDevOps.AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, DisplayName = "Personal Access Token", IsSecret = true }
                        ]
                    }
                ]
            },
            {
                WorkTrackingSystems.Jira,
                [
                    new AuthenticationMethod
                    {
                        Key = AuthenticationMethodKeys.JiraCloud,
                        DisplayName = "Jira Cloud (API Token)",
                        Options =
                        [
                            new AuthenticationMethodOption { Key = Jira.JiraWorkTrackingOptionNames.Url, DisplayName = "Jira URL", IsSecret = false },
                            new AuthenticationMethodOption { Key = Jira.JiraWorkTrackingOptionNames.Username, DisplayName = "Username (Email)", IsSecret = false },
                            new AuthenticationMethodOption { Key = Jira.JiraWorkTrackingOptionNames.ApiToken, DisplayName = "API Token", IsSecret = true }
                        ]
                    },
                    new AuthenticationMethod
                    {
                        Key = AuthenticationMethodKeys.JiraDataCenter,
                        DisplayName = "Jira Data Center (Personal Access Token)",
                        Options =
                        [
                            new AuthenticationMethodOption { Key = Jira.JiraWorkTrackingOptionNames.Url, DisplayName = "Jira URL", IsSecret = false },
                            new AuthenticationMethodOption { Key = Jira.JiraWorkTrackingOptionNames.ApiToken, DisplayName = "Personal Access Token", IsSecret = true }
                        ]
                    }
                ]
            },
            {
                WorkTrackingSystems.Linear,
                [
                    new AuthenticationMethod
                    {
                        Key = AuthenticationMethodKeys.LinearApiKey,
                        DisplayName = "API Key",
                        Options =
                        [
                            new AuthenticationMethodOption { Key = Linear.LinearWorkTrackingOptionNames.ApiKey, DisplayName = "API Key", IsSecret = true }
                        ]
                    }
                ]
            },
            {
                WorkTrackingSystems.Csv,
                [
                    new AuthenticationMethod
                    {
                        Key = AuthenticationMethodKeys.None,
                        DisplayName = "No Authentication",
                        Options = []
                    }
                ]
            }
        };

        public static List<AuthenticationMethod> GetMethodsForSystem(WorkTrackingSystems system)
        {
            return MethodsBySystem.TryGetValue(system, out var methods)
                ? methods
                : throw new NotSupportedException($"No authentication methods defined for {system}");
        }

        public static AuthenticationMethod? GetMethodByKey(WorkTrackingSystems system, string key)
        {
            return GetMethodsForSystem(system).FirstOrDefault(m => m.Key == key);
        }

        public static string GetDisplayName(WorkTrackingSystems system, string key)
        {
            var method = GetMethodByKey(system, key);
            return method?.DisplayName ?? key;
        }
    }
}
