using Lighthouse.Backend.Models.Auth;

namespace Lighthouse.Backend.Startup
{
    public static class AuthPostureBanner
    {
        private const string AuthenticationLabel = "Authentication";
        private const string AuthorizationLabel = "Authorization";
        private const string EmergencyAdminLabel = "Emergency Admin";
        private const string Enabled = "Enabled";
        private const string Disabled = "Disabled";

        public static IReadOnlyList<string> BuildAuthPostureLines(IConfiguration configuration)
        {
            var authentication = configuration.GetSection("Authentication").Get<AuthenticationConfiguration>() ?? new AuthenticationConfiguration();
            var authorization = configuration.GetSection("Authorization").Get<AuthorizationConfiguration>() ?? new AuthorizationConfiguration();

            var lines = new List<string>
            {
                FormatLine("🔐", AuthenticationLabel, authentication.Enabled ? Enabled : Disabled),
                FormatLine("🛡️", AuthorizationLabel, authorization.Enabled ? Enabled : Disabled)
            };

            if (authorization.Enabled && authorization.EmergencySystemAdminSubjects.Count > 0)
            {
                lines.Add(FormatLine("🚨", EmergencyAdminLabel, string.Join(", ", authorization.EmergencySystemAdminSubjects)));
            }

            return lines;
        }

        private static string FormatLine(string emoji, string label, string value)
        {
            return $"{emoji}  {label,-13} : {value}";
        }
    }
}
