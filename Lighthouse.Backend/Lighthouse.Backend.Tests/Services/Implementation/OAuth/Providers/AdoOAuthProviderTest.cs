using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth.Providers;
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
    public class AdoOAuthProviderTest
    {
        private const string TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        private const string AuthorizeEndpointHost = "login.microsoftonline.com";
        private const string AuthorizeEndpointPath = "/common/oauth2/v2.0/authorize";
        private const string TenantGuid = "1f3d4c2b-0e7a-4d6f-9b8a-2c5e7f9a1b3d";
        private const string PerTenantAuthorizeEndpointPath = $"/{TenantGuid}/oauth2/v2.0/authorize";
        private const string PerTenantTokenEndpoint = $"https://login.microsoftonline.com/{TenantGuid}/oauth2/v2.0/token";

        private static readonly DateTimeOffset FixedNow = new(2026, 5, 16, 12, 0, 0, TimeSpan.Zero);

        [Test]
        public void ProviderKey_ReturnsAdoOAuthConstant()
        {
            var provider = CreateProvider(CreateHandler((_, _) => Task.FromResult(EmptyResponse())));

            Assert.That(provider.ProviderKey, Is.EqualTo("ado.oauth"));
        }

        private static readonly string[] ExpectedDefaultScopes =
        [
            "vso.work_write",
            "offline_access",
        ];

        [Test]
        public void DefaultScopes_ContainsAzureDevOpsWorkWriteAndOfflineAccess()
        {
            var provider = CreateProvider(CreateHandler((_, _) => Task.FromResult(EmptyResponse())));

            Assert.That(provider.DefaultScopes, Is.EquivalentTo(ExpectedDefaultScopes));
        }

        [Test]
        public void BuildAuthorizationUrl_ReturnsMicrosoftIdentityV2AuthorizeUriWithAllRequiredQueryParameters()
        {
            var provider = CreateProvider(CreateHandler((_, _) => Task.FromResult(EmptyResponse())));
            var context = CreateFlowContext();

            var url = provider.BuildAuthorizationUrl(context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(url.Host, Is.EqualTo(AuthorizeEndpointHost));
                Assert.That(url.AbsolutePath, Is.EqualTo(AuthorizeEndpointPath));
            }

            var queryParameters = HttpUtility.ParseQueryString(url.Query);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queryParameters["client_id"], Is.EqualTo("ado-client-id"));
                Assert.That(queryParameters["response_type"], Is.EqualTo("code"));
                Assert.That(queryParameters["redirect_uri"], Is.EqualTo("https://lighthouse.example.com/api/oauth/callback"));
                Assert.That(queryParameters["scope"], Is.EqualTo("vso.work_write offline_access"));
                Assert.That(queryParameters["state"], Is.EqualTo("signed-state-token"));
                Assert.That(queryParameters["response_mode"], Is.EqualTo("query"));
                Assert.That(queryParameters["prompt"], Is.EqualTo("consent"));
            }
        }

        [Test]
        public void BuildAuthorizationUrl_WhenTenantIdSet_TargetsPerTenantEndpoint()
        {
            var provider = CreateProvider(CreateHandler((_, _) => Task.FromResult(EmptyResponse())));
            var context = CreateFlowContext(tenantId: TenantGuid);

            var url = provider.BuildAuthorizationUrl(context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(url.Host, Is.EqualTo(AuthorizeEndpointHost));
                Assert.That(url.AbsolutePath, Is.EqualTo(PerTenantAuthorizeEndpointPath));
                Assert.That(url.AbsolutePath, Does.Not.Contain("/common/"));
            }
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void BuildAuthorizationUrl_WhenTenantIdNullOrBlank_TargetsCommonEndpoint(string? tenantId)
        {
            var provider = CreateProvider(CreateHandler((_, _) => Task.FromResult(EmptyResponse())));
            var context = CreateFlowContext(tenantId: tenantId);

            var url = provider.BuildAuthorizationUrl(context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(url.Host, Is.EqualTo(AuthorizeEndpointHost));
                Assert.That(url.AbsolutePath, Is.EqualTo(AuthorizeEndpointPath));
            }
        }

        [Test]
        public async Task ExchangeCodeAsync_WhenTenantIdSet_PostsToPerTenantTokenEndpoint()
        {
            HttpRequestMessage? capturedRequest = null;
            var handler = CreateHandler((request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(SuccessTokenResponse("a", "r", 3600));
            });

            var provider = CreateProvider(handler);
            var context = CreateFlowContext(tenantId: TenantGuid);

            await provider.ExchangeCodeAsync("code", context, CancellationToken.None);

            Assert.That(capturedRequest!.RequestUri!.ToString(), Is.EqualTo(PerTenantTokenEndpoint));
        }

        [Test]
        public async Task RefreshTokenAsync_WhenTenantIdSet_PostsToPerTenantTokenEndpoint()
        {
            HttpRequestMessage? capturedRequest = null;
            var handler = CreateHandler((request, _) =>
            {
                capturedRequest = request;
                return Task.FromResult(SuccessTokenResponse("a", "r", 3600));
            });

            var provider = CreateProvider(handler);
            var refreshContext = new OAuthRefreshContext("old-refresh", "ado-client-id", "ado-client-secret", TenantGuid);

            await provider.RefreshTokenAsync(refreshContext, CancellationToken.None);

            Assert.That(capturedRequest!.RequestUri!.ToString(), Is.EqualTo(PerTenantTokenEndpoint));
        }

        [Test]
        public async Task ExchangeCodeAsync_PostsAuthorizationCodeGrantToMicrosoftTokenEndpointAndParsesTokens()
        {
            HttpRequestMessage? capturedRequest = null;
            string? capturedBody = null;

            var handler = CreateHandler(async (request, ct) =>
            {
                capturedRequest = request;
                capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
                return SuccessTokenResponse(
                    accessToken: "ado-access-token",
                    refreshToken: "ado-refresh-token",
                    expiresInSeconds: 3600);
            });

            var provider = CreateProvider(handler);
            var context = CreateFlowContext();

            var tokens = await provider.ExchangeCodeAsync("ado-auth-code", context, CancellationToken.None);

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
                Assert.That(formParameters["client_id"].ToString(), Is.EqualTo("ado-client-id"));
                Assert.That(formParameters["client_secret"].ToString(), Is.EqualTo("ado-client-secret"));
                Assert.That(formParameters["code"].ToString(), Is.EqualTo("ado-auth-code"));
                Assert.That(formParameters["redirect_uri"].ToString(), Is.EqualTo("https://lighthouse.example.com/api/oauth/callback"));
                Assert.That(formParameters["scope"].ToString(), Is.EqualTo("vso.work_write offline_access"));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tokens.AccessToken, Is.EqualTo("ado-access-token"));
                Assert.That(tokens.RefreshToken, Is.EqualTo("ado-refresh-token"));
                Assert.That(tokens.ExpiresAt, Is.EqualTo(FixedNow.AddSeconds(3600)));
            }
        }

        [Test]
        public void ExchangeCodeAsync_TokenEndpointReturnsNon2xx_ThrowsOAuthProviderResponseException()
        {
            const string errorBody = "{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS70008: The provided authorization code has expired\"}";
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
                Assert.That(ex!.ProviderKey, Is.EqualTo("ado.oauth"));
                Assert.That(ex.HttpStatus, Is.EqualTo(400));
                Assert.That(ex.IdpErrorCode, Is.EqualTo("invalid_grant"));
                Assert.That(ex.IdpErrorDescription, Is.EqualTo("AADSTS70008: The provided authorization code has expired"));
            }
        }

        [Test]
        public async Task RefreshTokenAsync_PostsRefreshTokenGrantToMicrosoftTokenEndpointAndParsesTokens()
        {
            string? capturedBody = null;
            HttpRequestMessage? capturedRequest = null;

            var handler = CreateHandler(async (request, ct) =>
            {
                capturedRequest = request;
                capturedBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
                return SuccessTokenResponse(
                    accessToken: "new-ado-access-token",
                    refreshToken: "new-ado-refresh-token",
                    expiresInSeconds: 7200);
            });

            var provider = CreateProvider(handler);
            var refreshContext = new OAuthRefreshContext("old-ado-refresh-token", "ado-client-id", "ado-client-secret");

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
                Assert.That(formParameters["refresh_token"].ToString(), Is.EqualTo("old-ado-refresh-token"));
                Assert.That(formParameters["client_id"].ToString(), Is.EqualTo("ado-client-id"));
                Assert.That(formParameters["client_secret"].ToString(), Is.EqualTo("ado-client-secret"));
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tokens.AccessToken, Is.EqualTo("new-ado-access-token"));
                Assert.That(tokens.RefreshToken, Is.EqualTo("new-ado-refresh-token"));
                Assert.That(tokens.ExpiresAt, Is.EqualTo(FixedNow.AddSeconds(7200)));
            }
        }

        [Test]
        public void RefreshTokenAsync_TokenEndpointReturnsNon2xx_ThrowsOAuthProviderResponseException()
        {
            const string errorBody = "{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS700082: The refresh token has expired due to inactivity\"}";
            var handler = CreateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(errorBody, Encoding.UTF8, "application/json"),
            }));

            var provider = CreateProvider(handler);
            var refreshContext = new OAuthRefreshContext("revoked-refresh-token", "ado-client-id", "ado-client-secret");

            var ex = Assert.ThrowsAsync<OAuthProviderResponseException>(
                () => provider.RefreshTokenAsync(refreshContext, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ex!.ProviderKey, Is.EqualTo("ado.oauth"));
                Assert.That(ex.HttpStatus, Is.EqualTo(401));
                Assert.That(ex.IdpErrorCode, Is.EqualTo("invalid_grant"));
                Assert.That(ex.IdpErrorDescription, Is.EqualTo("AADSTS700082: The refresh token has expired due to inactivity"));
            }
        }

        private static AdoOAuthProvider CreateProvider(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            var timeProvider = new FakeTimeProvider(FixedNow);
            return new AdoOAuthProvider(httpClient, timeProvider, Microsoft.Extensions.Logging.Abstractions.NullLogger<AdoOAuthProvider>.Instance);
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
                + "\"scope\":\"vso.work_write offline_access\""
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
            "vso.work_write",
            "offline_access",
        ];

        private static OAuthFlowContext CreateFlowContext(string? tenantId = null)
        {
            return new OAuthFlowContext(
                ConnectionId: 42,
                ProviderKey: "ado.oauth",
                ClientId: "ado-client-id",
                ClientSecret: "ado-client-secret",
                RedirectUri: new Uri("https://lighthouse.example.com/api/oauth/callback"),
                State: "signed-state-token",
                Scopes: FlowContextScopes,
                TenantId: tenantId);
        }
    }
}
