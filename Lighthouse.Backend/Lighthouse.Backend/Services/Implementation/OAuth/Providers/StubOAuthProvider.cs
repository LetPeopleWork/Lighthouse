using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Microsoft.AspNetCore.WebUtilities;

namespace Lighthouse.Backend.Services.Implementation.OAuth.Providers
{
    /// <summary>
    /// Test-only OAuth provider used by integration tests and the Playwright walking
    /// skeleton to exercise the full /api/oauth/{provider}/connect → callback round-trip
    /// without contacting a real external IdP. Registered in DI only when
    /// Lighthouse:OAuth:UseStubProvider=true; never present in a production build.
    /// </summary>
    public class StubOAuthProvider : IOAuthProvider
    {
        private const string CallbackPath = "/api/oauth/callback";

        private static readonly IReadOnlyList<string> StubDefaultScopes = ["stub.read"];

        private readonly IServiceConfig serviceConfig;
        private readonly TimeProvider timeProvider;

        public StubOAuthProvider(IServiceConfig serviceConfig, TimeProvider timeProvider)
        {
            ArgumentNullException.ThrowIfNull(serviceConfig);
            ArgumentNullException.ThrowIfNull(timeProvider);

            this.serviceConfig = serviceConfig;
            this.timeProvider = timeProvider;
        }

        public string ProviderKey => AuthenticationMethodKeys.StubOAuth;

        public IReadOnlyList<string> DefaultScopes => StubDefaultScopes;

        public Uri BuildAuthorizationUrl(OAuthFlowContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var baseUrl = string.IsNullOrWhiteSpace(serviceConfig.BaseUrl)
                ? "http://localhost"
                : serviceConfig.BaseUrl;

            var queryParameters = new Dictionary<string, string?>
            {
                ["provider"] = ProviderKey,
                ["code"] = $"stub-test-code-{Guid.NewGuid():N}",
                ["state"] = context.State,
            };

            var callbackUrl = new Uri(new Uri(baseUrl), CallbackPath).ToString();
            var url = QueryHelpers.AddQueryString(callbackUrl, queryParameters);
            return new Uri(url);
        }

        public Task<OAuthTokens> ExchangeCodeAsync(string code, OAuthFlowContext context, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);
            ArgumentNullException.ThrowIfNull(context);

            var tokens = new OAuthTokens(
                $"stub-access-{Guid.NewGuid():N}",
                $"stub-refresh-{Guid.NewGuid():N}",
                timeProvider.GetUtcNow().AddHours(1));

            return Task.FromResult(tokens);
        }

        public Task<OAuthTokens> RefreshTokenAsync(OAuthRefreshContext context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(context);

            var tokens = new OAuthTokens(
                $"stub-access-{Guid.NewGuid():N}",
                $"stub-refresh-{Guid.NewGuid():N}",
                timeProvider.GetUtcNow().AddHours(1));

            return Task.FromResult(tokens);
        }
    }
}
