using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Security
{
    [NonParallelizable]
    public class S5_ApiKeyScopesTests
    {
        private const string CreateApiKeyPath = "/api/v1/apikeys";
        private const string SummaryPath = "/api/v1/worktrackingsystemconnections/summary";
        private const int InScopePortfolioId = 42;
        private const int OutOfScopePortfolioId = 99;
        private const int SystemAdminSubjectScopedTeamId = 10;

        private const string SystemAdminSubject = "system-admin-subject";
        private const string TeamAdminSubject = "team-admin-subject";
        private const string ViewerSubject = "viewer-subject";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient apiClient = null!;

        [SetUp]
        public void SetUp()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = WithS5Authentication(rootFactory);
            apiClient = factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            apiClient.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task S5_WalkingSkeleton_CreateScopedKey_ThenReadInScopePortfolio_Succeeds()
        {
            SeedSystemAdminAndPortfolios(SystemAdminSubject);

            var plainTextKey = await CreateScopedKeyAsSystemAdmin(new ApiKeyScopeDto
            {
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = InScopePortfolioId,
            });

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);

            var response = await keyClient.GetAsync($"/api/v1/portfolios/{InScopePortfolioId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task S5_ScopedKey_ReadOutOfScopePortfolio_Returns404()
        {
            SeedSystemAdminAndPortfolios(SystemAdminSubject);

            var plainTextKey = await CreateScopedKeyAsSystemAdmin(new ApiKeyScopeDto
            {
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = InScopePortfolioId,
            });

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);

            var response = await keyClient.GetAsync($"/api/v1/portfolios/{OutOfScopePortfolioId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task S5_ScopedReadKey_AttemptWriteAction_Returns403()
        {
            SeedSystemAdminAndPortfolios(SystemAdminSubject);

            var plainTextKey = await CreateScopedKeyAsSystemAdmin(new ApiKeyScopeDto
            {
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = InScopePortfolioId,
            });

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);

            var response = await keyClient.DeleteAsync($"/api/v1/portfolios/{InScopePortfolioId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task S5_KeyWithNoScopeRows_InheritsOwnerPermissions_BackwardsCompatibility()
        {
            SeedSystemAdminAndPortfolios(SystemAdminSubject);

            var plainTextKey = await CreateApiKeyAsSystemAdmin(scope: null);

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);

            var inScopeResponse = await keyClient.GetAsync($"/api/v1/portfolios/{InScopePortfolioId}");
            var outOfScopeResponse = await keyClient.GetAsync($"/api/v1/portfolios/{OutOfScopePortfolioId}");

            Assert.Multiple(() =>
            {
                Assert.That(inScopeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(outOfScopeResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public async Task S5_IssueTimeSupersetCheck_NonAdminRequestsSystemAdminScope_Returns403()
        {
            SeedTeamAdminAndSystemAdmin(TeamAdminSubject, SystemAdminSubject);

            using var teamAdminClient = factory.CreateClient();
            teamAdminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, TeamAdminSubject);

            var request = new CreateApiKeyRequest
            {
                Name = "escalation-attempt",
                Description = "should fail",
                Scope = new[]
                {
                    new ApiKeyScopeDto
                    {
                        Role = UserRole.SystemAdmin,
                        ScopeType = PermissionScopeType.System,
                        ScopeId = null,
                    },
                },
            };

            var response = await teamAdminClient.PostAsJsonAsync(CreateApiKeyPath, request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task S5_AnyScopedAdminRequirement_AdminCallerSatisfies_ViewerCallerRejected()
        {
            SeedTeamAdminAndSystemAdmin(TeamAdminSubject, SystemAdminSubject);
            SeedViewer(ViewerSubject);

            using var teamAdminClient = factory.CreateClient();
            teamAdminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, TeamAdminSubject);
            using var viewerClient = factory.CreateClient();
            viewerClient.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, ViewerSubject);

            var teamAdminResponse = await teamAdminClient.GetAsync(SummaryPath);
            var viewerResponse = await viewerClient.GetAsync(SummaryPath);

            Assert.Multiple(() =>
            {
                Assert.That(teamAdminResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(viewerResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            });
        }

        private async Task<string> CreateScopedKeyAsSystemAdmin(ApiKeyScopeDto scopeEntry)
        {
            return await CreateApiKeyAsSystemAdmin(new[] { scopeEntry });
        }

        private async Task<string> CreateApiKeyAsSystemAdmin(IReadOnlyList<ApiKeyScopeDto>? scope)
        {
            using var adminClient = factory.CreateClient();
            adminClient.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, SystemAdminSubject);

            var request = new CreateApiKeyRequest
            {
                Name = "test-key",
                Description = "for tests",
                Scope = scope,
            };

            var response = await adminClient.PostAsJsonAsync(CreateApiKeyPath, request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "API key creation precondition");

            var body = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("plainTextKey").GetString()!;
        }

        private void SeedSystemAdminAndPortfolios(string subject)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var profile = new UserProfile
            {
                Subject = subject,
                SubjectClaimType = "sub",
                DisplayName = "System Admin",
                Email = "sa@example.test",
            };
            dbContext.UserProfiles.Add(profile);

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Test Connection",
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
                Name = "In-Scope Portfolio",
                WorkTrackingSystemConnectionId = connection.Id,
            });
            dbContext.Portfolios.Add(new Portfolio
            {
                Id = OutOfScopePortfolioId,
                Name = "Out-Of-Scope Portfolio",
                WorkTrackingSystemConnectionId = connection.Id,
            });

            dbContext.SaveChanges();
        }

        private void SeedTeamAdminAndSystemAdmin(string teamAdminSubject, string systemAdminSubject)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var systemAdminProfile = new UserProfile
            {
                Subject = systemAdminSubject,
                SubjectClaimType = "sub",
                DisplayName = "System Admin",
                Email = "sa@example.test",
            };
            var teamAdminProfile = new UserProfile
            {
                Subject = teamAdminSubject,
                SubjectClaimType = "sub",
                DisplayName = "Team Admin",
                Email = "ta@example.test",
            };
            dbContext.UserProfiles.AddRange(systemAdminProfile, teamAdminProfile);

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = "pat",
            };
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = systemAdminProfile.Id,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = teamAdminProfile.Id,
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = SystemAdminSubjectScopedTeamId,
            });

            dbContext.SaveChanges();
        }

        private void SeedViewer(string subject)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var profile = new UserProfile
            {
                Subject = subject,
                SubjectClaimType = "sub",
                DisplayName = "Viewer",
                Email = "v@example.test",
            };
            dbContext.UserProfiles.Add(profile);
            dbContext.SaveChanges();
        }

        private static WebApplicationFactory<Program> WithS5Authentication(TestWebApplicationFactory<Program> root)
        {
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "true",
                        ["Authorization:Enabled"] = "true",
                    });
                });

                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(defaultOptions =>
                    {
                        defaultOptions.DefaultScheme = S5PolicyScheme;
                        defaultOptions.DefaultAuthenticateScheme = S5PolicyScheme;
                        defaultOptions.DefaultChallengeScheme = S5PolicyScheme;
                        defaultOptions.DefaultForbidScheme = S5PolicyScheme;
                    })
                    .AddPolicyScheme(S5PolicyScheme, "S5 Policy Scheme", options =>
                    {
                        options.ForwardDefaultSelector = ctx =>
                            ctx.Request.Headers.ContainsKey("X-Api-Key")
                                ? "LighthouseApiKey"
                                : TestAuthHandler.SchemeName;
                        options.ForwardChallenge = TestAuthHandler.SchemeName;
                        options.ForwardForbid = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { })
                    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                        "LighthouseApiKey",
                        _ => { });

                    var licenseServiceMock = new Mock<ILicenseService>();
                    licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);
                });
            });
        }

        private const string S5PolicyScheme = "S5PolicyScheme";
    }
}
