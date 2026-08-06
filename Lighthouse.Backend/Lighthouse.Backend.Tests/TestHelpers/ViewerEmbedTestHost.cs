using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Epic 5146 slice 01 (#5692) — ADR-132, viewer-identity embed session.
    ///
    /// Differs from <see cref="EmbedSessionTestHost"/> in one way that matters: there is no
    /// TestAuthHandler. Hop 1 only counts a principal authenticated on the ordinary cookie scheme
    /// (D56), and a header-borne test scheme never reaches that branch — so an interactive session
    /// here is a real <c>.Lighthouse.Session</c> cookie, protected with the host's own data
    /// protector and read back by the production cookie handler.
    /// </summary>
    public sealed class ViewerEmbedTestHost : IDisposable
    {
        public const string StartPath = "/embed/start";
        public const string HandshakePath = "/api/v1/embed/handshake";
        public const string EntryPath = "/embed/enter";

        public const string TeamsPath = "/api/v1/teams";
        public const string SessionStatusPath = "/api/v1/auth/session";

        public const string SessionCookieName = SmartAuthSchemeSelector.SessionCookieName;
        public const string EmbedCookieName = SmartAuthSchemeSelector.EmbedCookieName;

        public const string GroupClaimName = "groups";
        public const string StubbedAuthorizationEndpoint = "https://example.test/oidc/authorize";

        public const string SystemAdminSubject = "viewer-embed-system-admin";
        public const string GroupMappedViewerSubject = "viewer-embed-group-mapped";
        public const string ExplicitViewerSubject = "viewer-embed-explicit";
        public const string UnprovisionedViewerSubject = "viewer-embed-unprovisioned";

        public const string ViewerGroupValue = "lighthouse-viewers";

        public const int GroupMappedTeamId = 5692;
        public const int ExplicitTeamId = 5693;

        private const string CookieProtectorPurpose =
            "Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware";

        private readonly TestWebApplicationFactory<Program> root;
        private readonly List<WebApplicationFactory<Program>> derivedHosts = [];

        public ViewerEmbedTestHost()
        {
            root = new TestWebApplicationFactory<Program>();
            AuthEnabled = BuildHost(configuredAuthority: true, premiumLicence: true);
            AuthDisabled = BuildHost(configuredAuthority: true, premiumLicence: true, authenticationEnabled: false);
            LicenceBlocked = BuildHost(configuredAuthority: true, premiumLicence: false);
            Misconfigured = BuildHost(configuredAuthority: false, premiumLicence: true);
        }

        public WebApplicationFactory<Program> AuthEnabled { get; }

        public WebApplicationFactory<Program> AuthDisabled { get; }

        public WebApplicationFactory<Program> LicenceBlocked { get; }

        /// <summary>Authentication enabled with no authority — <see cref="AuthMode.Misconfigured"/> (DQ-7).</summary>
        public WebApplicationFactory<Program> Misconfigured { get; }

        public CapturedLogEvents LogEvents { get; } = new();

        public static HttpClient CreateClient(WebApplicationFactory<Program> host)
        {
            return host.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false,
            });
        }

        public static string NewNonce()
        {
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        public static async Task<HttpResponseMessage> StartAsync(
            WebApplicationFactory<Program> host,
            string? nonce,
            string? sessionCookie = null,
            string? embedCookie = null)
        {
            using var client = CreateClient(host);
            ApplyCookies(client, sessionCookie, embedCookie);

            var path = nonce is null
                ? StartPath
                : $"{StartPath}?nonce={Uri.EscapeDataString(nonce)}";

            return await client.GetAsync(path);
        }

        public static async Task<HandshakeReading> PollHandshakeAsync(WebApplicationFactory<Program> host, string nonce)
        {
            using var client = CreateClient(host);
            using var response = await client.GetAsync($"{HandshakePath}/{Uri.EscapeDataString(nonce)}");
            var body = await response.Content.ReadAsStringAsync();

            return new HandshakeReading((int)response.StatusCode, body);
        }

        public static async Task<HttpResponseMessage> EnterAsync(WebApplicationFactory<Program> host, string token)
        {
            using var client = CreateClient(host);
            return await client.GetAsync($"{EntryPath}?token={Uri.EscapeDataString(token)}");
        }

        public static async Task<HttpResponseMessage> GetAsViewerAsync(
            WebApplicationFactory<Program> host,
            string path,
            string? sessionCookie = null,
            string? embedCookie = null)
        {
            using var client = CreateClient(host);
            ApplyCookies(client, sessionCookie, embedCookie);
            return await client.GetAsync(path);
        }

        public static string? ReadCookieValue(HttpResponseMessage response, string cookieName)
        {
            if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            {
                return null;
            }

            var setCookie = values.FirstOrDefault(value => value.StartsWith($"{cookieName}=", StringComparison.Ordinal));
            if (setCookie is null)
            {
                return null;
            }

            return setCookie.Split(';', 2)[0][(cookieName.Length + 1)..];
        }

        /// <summary>
        /// A real session cookie for the ordinary scheme, protected by the host's own data protector,
        /// so the production cookie handler reads it exactly as it reads one issued by an OIDC login.
        /// The identity's authentication type mirrors production: the OIDC handler builds the identity
        /// and signs it into the cookie scheme, so the ticket — not the identity — names the scheme.
        /// </summary>
        public string ForgeInteractiveSessionCookie(
            WebApplicationFactory<Program> host,
            string subject,
            string displayName,
            params string[] groupValues)
        {
            ArgumentNullException.ThrowIfNull(host);
            ArgumentNullException.ThrowIfNull(groupValues);

            var claims = new List<Claim>
            {
                new("sub", subject),
                new("name", displayName),
            };

            foreach (var groupValue in groupValues)
            {
                claims.Add(new Claim(GroupClaimName, groupValue));
            }

            var identity = new ClaimsIdentity(claims, OpenIdConnectDefaults.AuthenticationScheme, "name", ClaimTypes.Role);
            var properties = new AuthenticationProperties
            {
                IssuedUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1),
                IsPersistent = false,
            };

            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                properties,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var protector = host.Services
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(CookieProtectorPurpose, CookieAuthenticationDefaults.AuthenticationScheme, "v2");

            return new TicketDataFormat(protector).Protect(ticket);
        }

        public void SeedRbacFixture()
        {
            using var scope = root.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            AddProfile(dbContext, SystemAdminSubject, "Viewer Embed System Admin", snapshot: null);
            dbContext.SaveChanges();

            var systemAdmin = dbContext.UserProfiles.Single(p => p.Subject == SystemAdminSubject);
            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = systemAdmin.Id,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });

            // The snapshot exists because hop 1 is a real OIDC login and
            // WriteGroupSnapshotOnTokenValidatedAsync writes it; a forged cookie skips that event.
            AddProfile(
                dbContext,
                GroupMappedViewerSubject,
                "Group Mapped Viewer",
                snapshot: JsonSerializer.Serialize(new[] { ViewerGroupValue }));
            AddProfile(dbContext, ExplicitViewerSubject, "Explicitly Permissioned Viewer", snapshot: null);
            AddProfile(dbContext, UnprovisionedViewerSubject, "Unprovisioned Viewer", snapshot: null);

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Viewer Embed Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = "pat",
            };
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            dbContext.Teams.Add(new Team
            {
                Id = GroupMappedTeamId,
                Name = "Group Mapped Team",
                WorkTrackingSystemConnectionId = connection.Id,
            });
            dbContext.Teams.Add(new Team
            {
                Id = ExplicitTeamId,
                Name = "Explicitly Granted Team",
                WorkTrackingSystemConnectionId = connection.Id,
            });
            dbContext.SaveChanges();

            dbContext.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = ViewerGroupValue,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = GroupMappedTeamId,
            });

            var explicitViewer = dbContext.UserProfiles.Single(p => p.Subject == ExplicitViewerSubject);
            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = explicitViewer.Id,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = ExplicitTeamId,
            });

            dbContext.SaveChanges();
        }

        public async Task DeleteViewerAsync(string subject)
        {
            using var scope = root.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var profile = dbContext.UserProfiles.Single(p => p.Subject == subject);

            var administration = scope.ServiceProvider.GetRequiredService<IRbacAdministrationService>();

            // D58: there is no deactivation in Lighthouse. Deletion is the control.
            await administration.DeleteUserAsync(profile.Id, CancellationToken.None);
        }

        public List<EmbedSessionToken> ReadEmbedSessionTokens()
        {
            using var scope = root.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return dbContext.EmbedSessionTokens.AsNoTracking().ToList();
        }

        public void Dispose()
        {
            foreach (var host in derivedHosts)
            {
                host.Dispose();
            }

            root.Dispose();
        }

        private static void ApplyCookies(HttpClient client, string? sessionCookie, string? embedCookie)
        {
            var cookies = new List<string>();
            if (sessionCookie is not null)
            {
                cookies.Add($"{SessionCookieName}={sessionCookie}");
            }

            if (embedCookie is not null)
            {
                cookies.Add($"{EmbedCookieName}={embedCookie}");
            }

            if (cookies.Count == 0)
            {
                return;
            }

            client.DefaultRequestHeaders.Remove("Cookie");
            client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies));
        }

        private static void AddProfile(LighthouseAppContext dbContext, string subject, string displayName, string? snapshot)
        {
            dbContext.UserProfiles.Add(new UserProfile
            {
                Subject = subject,
                SubjectClaimType = "sub",
                DisplayName = displayName,
                Email = $"{subject}@example.test",
                LastKnownGroupClaimValues = snapshot,
            });
        }

        private WebApplicationFactory<Program> BuildHost(
            bool configuredAuthority,
            bool premiumLicence,
            bool authenticationEnabled = true)
        {
            var host = root.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Enabled", authenticationEnabled ? "true" : "false");
                builder.UseSetting("Authentication:ClientId", "lighthouse-viewer-embed-test");
                builder.UseSetting("Authentication:ClientSecret", "test-secret");
                builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
                builder.UseSetting("Authentication:AllowedOrigins:0", "https://lighthouse.test");
                builder.UseSetting("Authorization:Enabled", "true");
                builder.UseSetting("Authorization:GroupClaimName", GroupClaimName);

                if (configuredAuthority)
                {
                    builder.UseSetting("Authentication:Authority", "https://example.test/oidc");
                    builder.UseSetting(
                        "Authentication:MetadataAddress",
                        "https://example.test/oidc/.well-known/openid-configuration");
                }

                builder.ConfigureLogging(logging => logging.AddProvider(LogEvents));

                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(new UnservedSpaPageStartupFilter());

                    // The identity provider is the one external, non-deterministic port in this flow.
                    // Only its discovery document is stubbed, so the challenge itself stays production.
                    services.PostConfigure<OpenIdConnectOptions>(
                        OpenIdConnectDefaults.AuthenticationScheme,
                        options =>
                        {
                            var configuration = new OpenIdConnectConfiguration
                            {
                                Issuer = "https://example.test/oidc",
                                AuthorizationEndpoint = StubbedAuthorizationEndpoint,
                                TokenEndpoint = "https://example.test/oidc/token",
                                EndSessionEndpoint = "https://example.test/oidc/logout",
                                JwksUri = "https://example.test/oidc/jwks",
                            };

                            // Both, as ForwardedHeadersOidcTestHost does: this hook runs after the
                            // framework's own post-configure, which has already built an HTTP
                            // configuration manager from MetadataAddress. Setting only Configuration
                            // leaves the stub inert and the challenge reaches the network.
                            options.Configuration = configuration;
                            options.ConfigurationManager =
                                new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                        });

                    var licenseServiceMock = new Mock<ILicenseService>();
                    licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(premiumLicence);
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);
                });
            });

            derivedHosts.Add(host);
            return host;
        }

        public sealed record HandshakeReading(int StatusCode, string Body)
        {
            public bool HasProperty(string propertyName)
            {
                if (string.IsNullOrWhiteSpace(Body))
                {
                    return false;
                }

                try
                {
                    using var document = JsonDocument.Parse(Body);
                    return document.RootElement.ValueKind == JsonValueKind.Object
                        && document.RootElement.TryGetProperty(propertyName, out _);
                }
                catch (JsonException)
                {
                    return false;
                }
            }

            public string ReadString(string propertyName)
            {
                using var document = JsonDocument.Parse(Body);
                return document.RootElement.GetProperty(propertyName).GetString() ?? string.Empty;
            }
        }

        /// <summary>D62: the lost race must be observable. The only server-side observable is the log.</summary>
        public sealed class CapturedLogEvents : ILoggerProvider
        {
            private readonly List<string> eventNames = [];
            private readonly Lock gate = new();

            public IReadOnlyList<string> EventNames
            {
                get
                {
                    lock (gate)
                    {
                        return [.. eventNames];
                    }
                }
            }

            public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

            public void Dispose()
            {
                // Nothing to release; the provider owns only an in-memory list.
            }

            private void Record(string eventName)
            {
                lock (gate)
                {
                    eventNames.Add(eventName);
                }
            }

            private sealed class CapturingLogger(CapturedLogEvents owner) : ILogger
            {
                public IDisposable? BeginScope<TState>(TState state)
                    where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(
                    LogLevel logLevel,
                    EventId eventId,
                    TState state,
                    Exception? exception,
                    Func<TState, Exception?, string> formatter)
                {
                    if (!string.IsNullOrEmpty(eventId.Name))
                    {
                        owner.Record(eventId.Name);
                    }
                }
            }
        }
    }
}
