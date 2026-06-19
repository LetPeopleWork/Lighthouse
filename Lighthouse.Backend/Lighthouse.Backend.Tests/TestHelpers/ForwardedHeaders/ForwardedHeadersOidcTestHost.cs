using System.Net;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders
{
    public sealed class ForwardedHeadersOidcTestHost : IDisposable
    {
        public const string AuthorizationEndpoint = "https://idp.example.test/authorize";
        public const string PublicHostName = "lighthouse.public.example";

        private const string LoginEndpoint = "/api/v1/auth/login";

        private readonly TestWebApplicationFactory<Program> rootFactory;
        private readonly WebApplicationFactory<Program> factory;
        private readonly HttpClient client;

        public ForwardedHeadersOidcTestHost(string trustedProxyIp, IPAddress simulatedRemoteIp)
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Enabled", "true");
                builder.UseSetting("Authentication:Authority", "https://idp.example.test");
                builder.UseSetting("Authentication:ClientId", "lighthouse-test");
                builder.UseSetting("Authentication:ClientSecret", "test-secret");
                builder.UseSetting("Authentication:MetadataAddress", "https://idp.example.test/.well-known/openid-configuration");
                builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
                builder.UseSetting("Authorization:Enabled", "true");
                builder.UseSetting("Authentication:AllowedOrigins:0", $"https://{PublicHostName}");
                builder.UseSetting("Authentication:TrustedProxies:0", trustedProxyIp);

                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(
                        new ForwardedHeadersTestStartupFilter(simulatedRemoteIp));

                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseService.Object);

                    services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, PreSeededOidcConfiguration>();
                });
            });

            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        public async Task<OidcChallengeResponse> ChallengeLoginAsync(string forwardedProto, string forwardedHost)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, LoginEndpoint);
            request.Headers.Add("X-Forwarded-For", "198.51.100.42");
            request.Headers.Add("X-Forwarded-Proto", forwardedProto);
            request.Headers.Add("X-Forwarded-Host", forwardedHost);

            using var response = await client.SendAsync(request);

            var location = response.Headers.Location?.ToString() ?? string.Empty;
            var setCookies = response.Headers.TryGetValues("Set-Cookie", out var cookies)
                ? cookies.ToList()
                : [];

            return new OidcChallengeResponse(response.StatusCode, location, setCookies);
        }

        public void Dispose()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        private sealed class PreSeededOidcConfiguration : IPostConfigureOptions<OpenIdConnectOptions>
        {
            public void PostConfigure(string? name, OpenIdConnectOptions options)
            {
                var configuration = new OpenIdConnectConfiguration
                {
                    Issuer = "https://idp.example.test",
                    AuthorizationEndpoint = AuthorizationEndpoint,
                    TokenEndpoint = "https://idp.example.test/token",
                };

                options.Configuration = configuration;
                options.ConfigurationManager =
                    new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            }
        }
    }
}
