namespace Lighthouse.Backend.Tests.TestHelpers
{
    public static class AuthenticatedHttpClientExtensions
    {
        private const string ApiKeyHeader = "X-Api-Key";

        public static HttpClient AsSystemAdmin(this HttpClient client, string subject = "test-admin")
            => ApplyTestIdentity(client, subject, ClaimsDrivenRbacAdministrationService.SystemAdminGrant);

        public static HttpClient AsTeamAdmin(this HttpClient client, int teamId, string subject = "test-team-admin")
            => ApplyTestIdentity(client, subject, $"{ClaimsDrivenRbacAdministrationService.TeamAdminGrantPrefix}{teamId}");

        public static HttpClient AsPortfolioAdmin(this HttpClient client, int portfolioId, string subject = "test-portfolio-admin")
            => ApplyTestIdentity(client, subject, $"{ClaimsDrivenRbacAdministrationService.PortfolioAdminGrantPrefix}{portfolioId}");

        public static HttpClient AsViewer(this HttpClient client, string subject = "test-viewer")
            => ApplyTestIdentity(client, subject, roles: null);

        public static HttpClient AsAnonymous(this HttpClient client)
        {
            client.DefaultRequestHeaders.Remove(TestAuthHandler.SubjectHeader);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.DisplayNameHeader);
            return client;
        }

        public static HttpClient WithApiKey(this HttpClient client, string apiKey)
        {
            client.DefaultRequestHeaders.Remove(ApiKeyHeader);
            client.DefaultRequestHeaders.Add(ApiKeyHeader, apiKey);
            return client;
        }

        private static HttpClient ApplyTestIdentity(HttpClient client, string subject, string? roles)
        {
            client.DefaultRequestHeaders.Remove(TestAuthHandler.SubjectHeader);
            client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
            client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject);
            if (!string.IsNullOrEmpty(roles))
            {
                client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
            }
            return client;
        }
    }
}
