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

#pragma warning disable S1075 // The stub provider only ships when Lighthouse:OAuth:UseStubProvider=true (test/dev). It needs a benign localhost fallback so integration tests that don't configure BaseUrl still produce a parsable redirect URI; this constant never reaches production builds.
        private const string FallbackBaseUrl = "http://localhost";
#pragma warning restore S1075

        private static readonly IReadOnlyList<string> StubDefaultScopes = ["stub.read"];

        private readonly IServiceConfig serviceConfig;
        private readonly TimeProvider timeProvider;
        private readonly string providerKey;

        public StubOAuthProvider(IServiceConfig serviceConfig, TimeProvider timeProvider)
            : this(serviceConfig, timeProvider, AuthenticationMethodKeys.StubOAuth)
        {
        }

        public StubOAuthProvider(IServiceConfig serviceConfig, TimeProvider timeProvider, string providerKey)
        {
            ArgumentNullException.ThrowIfNull(serviceConfig);
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);

            this.serviceConfig = serviceConfig;
            this.timeProvider = timeProvider;
            this.providerKey = providerKey;
        }

        public string ProviderKey => providerKey;

        public IReadOnlyList<string> DefaultScopes => StubDefaultScopes;

        public Uri BuildAuthorizationUrl(OAuthFlowContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var baseUrl = string.IsNullOrWhiteSpace(serviceConfig.BaseUrl)
                ? FallbackBaseUrl
                : serviceConfig.BaseUrl;

            var queryParameters = new Dictionary<string, string?>
            {
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
