using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth.Providers;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Web;

namespace Lighthouse.Backend.Tests.Services.Implementation.OAuth.Providers
{
    [TestFixture]
    public class JiraOAuthProviderTest
    {
        private const string TokenEndpoint = "https://auth.atlassian.com/oauth/token";
        private const string AuthorizeEndpoint = "https://auth.atlassian.com/authorize";

        private static readonly DateTimeOffset FixedNow = new(2026, 5, 14, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void ProviderKey_ReturnsJiraOAuthConstant()
        {
            var provider = CreateProvider(CreateHandler((_, _) => Task.FromResult(EmptyResponse())));

            Assert.That(provider.ProviderKey, Is.EqualTo(AuthenticationMethodKeys.JiraOAuth));
            Assert.That(provider.ProviderKey, Is.EqualTo("jira.oauth"));
        }

        private static readonly string[] ExpectedDefaultScopes =
        [
            "read:jira-work",
            "read:jira-user",
            "read:board-scope:jira-software",
            "read:sprint:jira-software",
            "read:issue:jira-software",
            "read:project:jira",
            "offline_access",
        ];

        [Test]
        public void DefaultScopes_ContainsRequiredAtlassianScopesIncludingJiraSoftwareGranularScopes()
        {
            var provider = CreateProvider(CreateHandler((_, _) => Task.FromResult(EmptyResponse())));

            Assert.That(provider.DefaultScopes, Is.EquivalentTo(ExpectedDefaultScopes));
        }

        [Test]
        public void BuildAuthorizationUrl_ReturnsAtlassianAuthorizeUriWithAllRequiredQueryParameters()
        {
            var provider = CreateProvider(CreateHandler((_, _) => Task.FromResult(EmptyResponse())));
            var context = CreateFlowContext();

            var url = provider.BuildAuthorizationUrl(context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(url.Host, Is.EqualTo("auth.atlassian.com"));
                Assert.That(url.AbsolutePath, Is.EqualTo("/authorize"));
            }

            var queryParameters = HttpUtility.ParseQueryString(url.Query);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queryParameters["audience"], Is.EqualTo("api.atlassian.com"));
                Assert.That(queryParameters["client_id"], Is.EqualTo("abc123"));
                Assert.That(queryParameters["scope"], Is.EqualTo("read:jira-work read:jira-user offline_access"));
                Assert.That(queryParameters["redirect_uri"], Is.EqualTo("https://lighthouse.example.com/api/oauth/callback"));
                Assert.That(queryParameters["state"], Is.EqualTo("signed-state-token"));
                Assert.That(queryParameters["response_type"], Is.EqualTo("code"));
                Assert.That(queryParameters["prompt"], Is.EqualTo("consent"));
            }

            Assert.That(url.Query, Does.Contain("redirect_uri=https%3A%2F%2Flighthouse.example.com%2Fapi%2Foauth%2Fcallback"));
            Assert.That(url.Query, Does.Contain("scope=read%3Ajira-work%20read%3Ajira-user%20offline_access").Or.Contain("scope=read%3Ajira-work+read%3Ajira-user+offline_access"));
        }

        [Test]
        public async Task ExchangeCodeAsync_PostsAuthorizationCodeGrantToTokenEndpointAndParsesTokens()
        {
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var handler = CreateHandler(async (request, ct) =>
            {
                capturedRequest = request;
                capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
                return SuccessTokenResponse(
                    accessToken: "access-token-789",
                    refreshToken: "refresh-token-789",
                    expiresInSeconds: 3600);
            });

            var provider = CreateProvider(handler);
            var context = CreateFlowContext();

            var tokens = await provider.ExchangeCodeAsync("auth-code-456", context, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(capturedRequest, Is.Not.Null);
                Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(capturedRequest.RequestUri!.ToString(), Is.EqualTo(TokenEndpoint));
                Assert.That(capturedRequest.Content!.Headers.ContentType!.MediaType, Is.EqualTo("application/x-www-form-urlencoded"));
            }

            Assert.That(capturedBody, Is.Not.Null);
            var formParameters = QueryHelpers.ParseQuery(capturedBody!);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(formParameters["grant_type"].ToString(), Is.EqualTo("authorization_code"));
                Assert.That(formParameters["client_id"].ToString(), Is.EqualTo("abc123"));
                Assert.That(formParameters["client_secret"].ToString(), Is.EqualTo("secret"));
                Assert.That(formParameters["code"].ToString(), Is.EqualTo("auth-code-456"));
                Assert.That(formParameters["redirect_uri"].ToString(), Is.EqualTo("https://lighthouse.example.com/api/oauth/callback"));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tokens.AccessToken, Is.EqualTo("access-token-789"));
                Assert.That(tokens.RefreshToken, Is.EqualTo("refresh-token-789"));
                Assert.That(tokens.ExpiresAt, Is.EqualTo(FixedNow.AddSeconds(3600)));
            }
        }

        [Test]
        public void ExchangeCodeAsync_TokenEndpointReturnsNon2xx_ThrowsOAuthProviderResponseException()
        {
            const string errorBody = "{\"error\":\"invalid_grant\",\"error_description\":\"Authorization code expired\"}";
            var handler = CreateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorBody, Encoding.UTF8, "application/json"),
            }));

            var provider = CreateProvider(handler);
            var context = CreateFlowContext();

            var ex = Assert.ThrowsAsync<OAuthProviderResponseException>(
                () => provider.ExchangeCodeAsync("expired-code", context, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.ProviderKey, Is.EqualTo("jira.oauth"));
                Assert.That(ex.HttpStatus, Is.EqualTo(400));
                Assert.That(ex.IdpErrorCode, Is.EqualTo("invalid_grant"));
                Assert.That(ex.IdpErrorDescription, Is.EqualTo("Authorization code expired"));
            }
        }

        [Test]
        public async Task RefreshTokenAsync_PostsRefreshTokenGrantToTokenEndpointAndParsesTokens()
        {
            string? capturedBody = null;
            HttpRequestMessage? capturedRequest = null;

            var handler = CreateHandler(async (request, ct) =>
            {
                capturedRequest = request;
                capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
                return SuccessTokenResponse(
                    accessToken: "new-access-token",
                    refreshToken: "new-refresh-token",
                    expiresInSeconds: 7200);
            });

            var provider = CreateProvider(handler);
            var refreshContext = new OAuthRefreshContext("old-refresh-token", "abc123", "secret");

            var tokens = await provider.RefreshTokenAsync(refreshContext, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(capturedRequest, Is.Not.Null);
                Assert.That(capturedRequest!.Method, Is.EqualTo(HttpMethod.Post));
                Assert.That(capturedRequest.RequestUri!.ToString(), Is.EqualTo(TokenEndpoint));
            }

            var formParameters = QueryHelpers.ParseQuery(capturedBody!);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(formParameters["grant_type"].ToString(), Is.EqualTo("refresh_token"));
                Assert.That(formParameters["refresh_token"].ToString(), Is.EqualTo("old-refresh-token"));
                Assert.That(formParameters["client_id"].ToString(), Is.EqualTo("abc123"));
                Assert.That(formParameters["client_secret"].ToString(), Is.EqualTo("secret"));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tokens.AccessToken, Is.EqualTo("new-access-token"));
                Assert.That(tokens.RefreshToken, Is.EqualTo("new-refresh-token"));
                Assert.That(tokens.ExpiresAt, Is.EqualTo(FixedNow.AddSeconds(7200)));
            }
        }

        [Test]
        public void RefreshTokenAsync_TokenEndpointReturnsNon2xx_ThrowsOAuthProviderResponseException()
        {
            const string errorBody = "{\"error\":\"invalid_token\",\"error_description\":\"Refresh token revoked\"}";
            var handler = CreateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(errorBody, Encoding.UTF8, "application/json"),
            }));

            var provider = CreateProvider(handler);
            var refreshContext = new OAuthRefreshContext("revoked-refresh-token", "abc123", "secret");

            var ex = Assert.ThrowsAsync<OAuthProviderResponseException>(
                () => provider.RefreshTokenAsync(refreshContext, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.ProviderKey, Is.EqualTo("jira.oauth"));
                Assert.That(ex.HttpStatus, Is.EqualTo(401));
                Assert.That(ex.IdpErrorCode, Is.EqualTo("invalid_token"));
                Assert.That(ex.IdpErrorDescription, Is.EqualTo("Refresh token revoked"));
            }
        }

        private JiraOAuthProvider CreateProvider(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            var timeProvider = new FakeTimeProvider(FixedNow);
            return new JiraOAuthProvider(httpClient, timeProvider, Microsoft.Extensions.Logging.Abstractions.NullLogger<JiraOAuthProvider>.Instance);
        }

        private static HttpMessageHandler CreateHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(respond);
            return mock.Object;
        }

        private static HttpResponseMessage SuccessTokenResponse(string accessToken, string refreshToken, int expiresInSeconds)
        {
            var body = "{"
                + $"\"access_token\":\"{accessToken}\","
                + $"\"refresh_token\":\"{refreshToken}\","
                + $"\"expires_in\":{expiresInSeconds},"
                + "\"token_type\":\"Bearer\","
                + "\"scope\":\"read:jira-work read:jira-user offline_access\""
                + "}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }

        private static HttpResponseMessage EmptyResponse()
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
            };
        }

        private static readonly string[] FlowContextScopes =
        [
            "read:jira-work",
            "read:jira-user",
            "offline_access",
        ];

        private static OAuthFlowContext CreateFlowContext()
        {
            return new OAuthFlowContext(
                ConnectionId: 42,
                ProviderKey: "jira.oauth",
                ClientId: "abc123",
                ClientSecret: "secret",
                RedirectUri: new Uri("https://lighthouse.example.com/api/oauth/callback"),
                State: "signed-state-token",
                Scopes: FlowContextScopes);
        }
    }
}
