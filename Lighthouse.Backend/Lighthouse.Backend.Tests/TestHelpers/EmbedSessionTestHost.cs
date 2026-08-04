using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    // Epic 5146 slice 02a (#5641) — ADR-129 / ADR-130 / ADR-131.
    public sealed class EmbedSessionTestHost : IDisposable
    {
        public const string ExchangePath = "/api/v1/embed/session-token";
        public const string RevokeAllPath = "/api/v1/embed/session-token/revoke-all";
        public const string EntryPath = "/embed/enter";

        public const string EmbedCookieName = ".Lighthouse.Embed";
        public const string EmbedCookieScheme = "LighthouseEmbedCookie";
        public const string SessionCookieName = ".Lighthouse.Session";
        public const string EmbedRateLimitPolicy = "EmbedSession";
        public const string TokenLifetimeConfigurationKey = "Embed:TokenLifetimeSeconds";

        public const string AuthModePath = "/api/v1/auth/mode";
        public const string SessionStatusPath = "/api/v1/auth/session";
        public const string MySummaryPath = "/api/v1/authorization/my-summary";

        public const string SystemAdminSubject = "embed-system-admin";
        public const int InScopePortfolioId = 4201;
        public const int OutOfScopePortfolioId = 4202;

        private const string CreateApiKeyPath = "/api/v1/apikeys";
        private const string PolicySchemeName = "EmbedTestPolicyScheme";
        private const string ProductionSmartScheme = "LighthouseSmartAuth";

        private readonly TestWebApplicationFactory<Program> root;
        private readonly List<WebApplicationFactory<Program>> derivedHosts = [];

        public EmbedSessionTestHost()
        {
            root = new TestWebApplicationFactory<Program>();
            AuthEnabled = BuildHost(authenticationEnabled: true, premiumLicence: true, extraSettings: null);
            AuthDisabled = BuildHost(authenticationEnabled: false, premiumLicence: true, extraSettings: null);
            LicenceBlocked = BuildHost(authenticationEnabled: true, premiumLicence: false, extraSettings: null);
        }

        public WebApplicationFactory<Program> AuthEnabled { get; }

        public WebApplicationFactory<Program> AuthDisabled { get; }

        public WebApplicationFactory<Program> LicenceBlocked { get; }

        public WebApplicationFactory<Program> WithEmbedRateLimit(int permitLimit, int windowSeconds)
        {
            var limits = new Dictionary<string, string?>
            {
                ["RateLimits:Enabled"] = "true",
                [$"RateLimits:Policies:{EmbedRateLimitPolicy}:PermitLimit"] = permitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [$"RateLimits:Policies:{EmbedRateLimitPolicy}:WindowSeconds"] = windowSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [$"RateLimits:Policies:{EmbedRateLimitPolicy}:QueueLimit"] = "0",
            };

            return BuildHost(authenticationEnabled: true, premiumLicence: true, extraSettings: limits);
        }

        public WebApplicationFactory<Program> WithTokenLifetime(int seconds)
        {
            var lifetime = new Dictionary<string, string?>
            {
                [TokenLifetimeConfigurationKey] = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };

            return BuildHost(authenticationEnabled: true, premiumLicence: true, extraSettings: lifetime);
        }

        public static HttpClient CreateClient(WebApplicationFactory<Program> host)
        {
            return host.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false,
            });
        }

        public void SeedSystemAdminAndPortfolios()
        {
            using var scope = root.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var profile = new UserProfile
            {
                Subject = SystemAdminSubject,
                SubjectClaimType = "sub",
                DisplayName = "Embed System Admin",
                Email = "embed-admin@example.test",
            };
            dbContext.UserProfiles.Add(profile);

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Embed Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = "pat",
            };
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = profile.Id,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });

            dbContext.Portfolios.Add(new Portfolio
            {
                Id = InScopePortfolioId,
                Name = "Embed In-Scope Portfolio",
                WorkTrackingSystemConnectionId = connection.Id,
            });
            dbContext.Portfolios.Add(new Portfolio
            {
                Id = OutOfScopePortfolioId,
                Name = "Embed Out-Of-Scope Portfolio",
                WorkTrackingSystemConnectionId = connection.Id,
            });

            dbContext.SaveChanges();
        }

        public async Task<string> CreateReadScopedKeyAsync(int portfolioId)
        {
            var scopeEntry = new ApiKeyScopeDto
            {
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = portfolioId,
            };

            return await CreateKeyAsync([scopeEntry]);
        }

        public async Task<string> CreateOwnerScopedKeyAsync()
        {
            return await CreateKeyAsync(scope: null);
        }

        public void UnlinkEveryApiKeyOwner()
        {
            using var scope = root.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            foreach (var apiKey in dbContext.ApiKeys.ToList())
            {
                apiKey.OwnerUserProfileId = null;
                apiKey.OwnerSubject = null;
                apiKey.CreatedByUser = string.Empty;
            }

            dbContext.SaveChanges();
        }

        public static async Task<HttpResponseMessage> ExchangeAsync(WebApplicationFactory<Program> host, string? apiKey)
        {
            using var client = CreateClient(host);
            if (apiKey is not null)
            {
                client.WithApiKey(apiKey);
            }

            return await client.PostAsync(ExchangePath, content: null);
        }

        public static async Task<string> MintTokenAsync(WebApplicationFactory<Program> host, string apiKey)
        {
            using var response = await ExchangeAsync(host, apiKey);
            Assert.That((int)response.StatusCode, Is.EqualTo(200), "precondition: the exchange must mint a token for a valid API key");

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("token").GetString()!;
        }

        public static async Task<HttpResponseMessage> EnterAsync(WebApplicationFactory<Program> host, string token, string? returnPath = null)
        {
            using var client = CreateClient(host);
            var path = returnPath is null
                ? $"{EntryPath}?token={Uri.EscapeDataString(token)}"
                : $"{EntryPath}?token={Uri.EscapeDataString(token)}&returnPath={Uri.EscapeDataString(returnPath)}";

            return await client.GetAsync(path);
        }

        public static string? ReadSetCookie(HttpResponseMessage response, string cookieName)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                return null;
            }

            return values.FirstOrDefault(value => value.StartsWith($"{cookieName}=", StringComparison.Ordinal));
        }

        public static string? ReadCookieValue(HttpResponseMessage response, string cookieName)
        {
            var setCookie = ReadSetCookie(response, cookieName);
            if (setCookie is null)
            {
                return null;
            }

            var firstSegment = setCookie.Split(';', 2)[0];
            return firstSegment[(cookieName.Length + 1)..];
        }

        public static HttpClient WithEmbedCookie(HttpClient client, string cookieValue)
        {
            client.DefaultRequestHeaders.Remove("Cookie");
            client.DefaultRequestHeaders.Add("Cookie", $"{EmbedCookieName}={cookieValue}");
            return client;
        }

        public async Task<string> EstablishEmbedCookieAsync(string apiKey)
        {
            var token = await MintTokenAsync(AuthEnabled, apiKey);
            using var response = await EnterAsync(AuthEnabled, token);

            var cookieValue = ReadCookieValue(response, EmbedCookieName);
            Assert.That(cookieValue, Is.Not.Null.And.Not.Empty, "precondition: the entry point must issue an embed cookie");
            return cookieValue!;
        }

        public void Dispose()
        {
            foreach (var host in derivedHosts)
            {
                host.Dispose();
            }

            root.Dispose();
        }

        private async Task<string> CreateKeyAsync(List<ApiKeyScopeDto>? scope)
        {
            using var adminClient = CreateClient(AuthEnabled);
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, SystemAdminSubject);

            var request = new CreateApiKeyRequest
            {
                Name = $"embed-test-key-{Guid.NewGuid():N}",
                Description = "epic 5146 slice 02a",
                Scope = scope,
            };

            var response = await adminClient.PostAsJsonAsync(CreateApiKeyPath, request);
            Assert.That((int)response.StatusCode, Is.EqualTo(201), "precondition: API key creation must succeed");

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("plainTextKey").GetString()!;
        }

        private WebApplicationFactory<Program> BuildHost(
            bool authenticationEnabled,
            bool premiumLicence,
            Dictionary<string, string?>? extraSettings)
        {
            var host = root.WithWebHostBuilder(builder =>
            {
                // UseSetting, not ConfigureAppConfiguration: Program reads builder.Configuration while
                // registering services, which is before an added configuration source would be visible.
                builder.UseSetting("Authentication:Enabled", authenticationEnabled ? "true" : "false");
                builder.UseSetting("Authentication:Authority", "https://example.test/oidc");
                builder.UseSetting("Authentication:ClientId", "lighthouse-embed-test");
                builder.UseSetting("Authentication:ClientSecret", "test-secret");
                builder.UseSetting("Authentication:MetadataAddress", "https://example.test/oidc/.well-known/openid-configuration");
                builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
                builder.UseSetting("Authentication:AllowedOrigins:0", "https://lighthouse.test");
                builder.UseSetting("Authorization:Enabled", authenticationEnabled ? "true" : "false");

                if (extraSettings is not null)
                {
                    foreach (var entry in extraSettings)
                    {
                        builder.UseSetting(entry.Key, entry.Value);
                    }
                }

                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(new UnservedSpaPageStartupFilter());

                    if (authenticationEnabled)
                    {
                        ConfigureTestAuthentication(services);
                    }

                    var licenseServiceMock = new Mock<ILicenseService>();
                    licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(premiumLicence);
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);
                });
            });

            derivedHosts.Add(host);
            return host;
        }

        // Only the seeded administrator is faked. Everything else — X-Api-Key, the embed cookie,
        // challenge and forbid — stays on the production LighthouseSmartAuth path, so a missing
        // SmartAuthSchemeSelector branch shows up as a failing test rather than being simulated here.
        private static void ConfigureTestAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(defaultOptions =>
            {
                defaultOptions.DefaultScheme = PolicySchemeName;
                defaultOptions.DefaultAuthenticateScheme = PolicySchemeName;
                defaultOptions.DefaultChallengeScheme = PolicySchemeName;
                defaultOptions.DefaultForbidScheme = PolicySchemeName;
            })
            .AddPolicyScheme(PolicySchemeName, "Embed Test Policy Scheme", options =>
            {
                options.ForwardDefaultSelector = ctx =>
                    ctx.Request.Headers.ContainsKey(TestAuthHandler.SubjectHeader)
                        ? TestAuthHandler.SchemeName
                        : ProductionSmartScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        }
    }
}
