using System.Net;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.Auth;
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
    [TestFixture]
    public class DeleteUnitRightsTests
    {
        private const int TeamId = 42;
        private const int PortfolioId = 77;
        private const string SystemAdminSubject = "system-admin-subject";
        private const string TeamAdminSubject = "team-admin-subject";
        private const string PortfolioAdminSubject = "portfolio-admin-subject";

        private const string SecurityPolicyScheme = "DeleteUnitRightsPolicyScheme";
        private const string ApiKeyScheme = "LighthouseApiKey";

        private static readonly string TeamRoute = $"/api/v1/teams/{TeamId}";
        private static readonly string PortfolioRoute = $"/api/v1/portfolios/{PortfolioId}";

        private readonly Mock<ILicenseService> licenseService = new();

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program>();
            factory = WithRealRbacService(rootFactory);
        }

        [SetUp]
        public void StartFromAnInstanceWithOneSystemAdminOneTeamAndOnePortfolio()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            rootFactory.Clock.Reset();
            rootFactory.UseFreshDatabase();
            factory.ForgetTheCachedMetrics();

            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            SeedTheInstance();
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            try
            {
                await SharedHostCleanup.DrainTheHistoryFillerOrFail(
                    factory.Services.GetRequiredService<OverTimeHistoryFiller>(), TimeSpan.FromSeconds(60));
                await SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(factory.Services, TimeSpan.FromSeconds(30));
            }
            finally
            {
                using var scope = rootFactory.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
            }
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            factory?.Dispose();
            rootFactory?.Dispose();
        }

        [Test]
        public async Task DeleteTeam_AsTeamAdmin_Returns403AndTheTeamSurvives()
        {
            SeedScopedAdmin(TeamAdminSubject, UserRole.TeamAdmin, PermissionScopeType.Team, TeamId);

            using var client = SessionOf(TeamAdminSubject);
            using var response = await client.DeleteAsync(TeamRoute);

            AssertRefusedAndTheTeamSurvives(response);
        }

        [Test]
        public async Task DeletePortfolio_AsPortfolioAdmin_Returns403AndThePortfolioSurvives()
        {
            SeedScopedAdmin(PortfolioAdminSubject, UserRole.PortfolioAdmin, PermissionScopeType.Portfolio, PortfolioId);

            using var client = SessionOf(PortfolioAdminSubject);
            using var response = await client.DeleteAsync(PortfolioRoute);

            AssertRefusedAndThePortfolioSurvives(response);
        }

        [TestCase(TeamAdminSubject)]
        [TestCase(SystemAdminSubject)]
        public async Task DeleteTeam_WithATeamScopedApiKey_Returns403AndTheTeamSurvives(string ownerSubject)
        {
            SeedScopedAdmin(TeamAdminSubject, UserRole.TeamAdmin, PermissionScopeType.Team, TeamId);
            var plainTextKey = await IssueTeamScopedKeyOwnedBy(ownerSubject);

            using var client = factory.CreateClient();
            client.WithApiKey(plainTextKey);
            using var response = await client.DeleteAsync(TeamRoute);

            AssertRefusedAndTheTeamSurvives(response);
        }

        [Test]
        public async Task DeleteTeam_AsSystemAdmin_Returns204AndTheTeamIsGone()
        {
            using var client = SessionOf(SystemAdminSubject);
            using var response = await client.DeleteAsync(TeamRoute);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(TheTeamExists(), Is.False);
            }
        }

        [Test]
        public async Task DeletePortfolio_AsSystemAdmin_Returns204AndThePortfolioIsGone()
        {
            using var client = SessionOf(SystemAdminSubject);
            using var response = await client.DeleteAsync(PortfolioRoute);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(ThePortfolioExists(), Is.False);
            }
        }

        [Test]
        public async Task DeleteTeam_AsSystemAdminWhilePremiumHasLapsed_Returns403AndTheTeamSurvives()
        {
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            using var client = SessionOf(SystemAdminSubject);
            using var response = await client.DeleteAsync(TeamRoute);

            AssertRefusedAndTheTeamSurvives(response);
        }

        [Test]
        public async Task DeletePortfolio_AsSystemAdminWhilePremiumHasLapsed_Returns403AndThePortfolioSurvives()
        {
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

            using var client = SessionOf(SystemAdminSubject);
            using var response = await client.DeleteAsync(PortfolioRoute);

            AssertRefusedAndThePortfolioSurvives(response);
        }

        private void AssertRefusedAndTheTeamSurvives(HttpResponseMessage response)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(TheTeamExists(), Is.True);
            }
        }

        private void AssertRefusedAndThePortfolioSurvives(HttpResponseMessage response)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(ThePortfolioExists(), Is.True);
            }
        }

        private HttpClient SessionOf(string subject)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject);
            return client;
        }

        private bool TheTeamExists()
        {
            using var scope = rootFactory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Teams.Any(team => team.Id == TeamId);
        }

        private bool ThePortfolioExists()
        {
            using var scope = rootFactory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Portfolios.Any(portfolio => portfolio.Id == PortfolioId);
        }

        private void SeedTheInstance()
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureCreated();

            var connection = new WorkTrackingSystemConnection
            {
                Name = "Test Connection",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = "pat",
            };
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            dbContext.Teams.Add(new Team
            {
                Id = TeamId,
                Name = "The Team",
                WorkTrackingSystemConnectionId = connection.Id,
            });
            dbContext.Portfolios.Add(new Portfolio
            {
                Id = PortfolioId,
                Name = "The Portfolio",
                WorkTrackingSystemConnectionId = connection.Id,
            });
            dbContext.SaveChanges();

            AddUserWithPermission(dbContext, SystemAdminSubject, UserRole.SystemAdmin, PermissionScopeType.System, scopeId: null);
        }

        private void SeedScopedAdmin(string subject, UserRole role, PermissionScopeType scopeType, int scopeId)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            AddUserWithPermission(dbContext, subject, role, scopeType, scopeId);
        }

        private static void AddUserWithPermission(
            LighthouseAppContext dbContext,
            string subject,
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId)
        {
            var profile = new UserProfile
            {
                Subject = subject,
                SubjectClaimType = "sub",
                DisplayName = subject,
                Email = $"{subject}@example.test",
            };
            dbContext.UserProfiles.Add(profile);
            dbContext.SaveChanges();

            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = profile.Id,
                Role = role,
                ScopeType = scopeType,
                ScopeId = scopeId,
            });
            dbContext.SaveChanges();
        }

        private async Task<string> IssueTeamScopedKeyOwnedBy(string ownerSubject)
        {
            using var scope = rootFactory.Services.CreateScope();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();

            var creationResult = await apiKeyService.CreateApiKeyAsync(
                name: $"team-scoped-key-{Guid.NewGuid():N}",
                description: "limited to one Team",
                createdByUser: ownerSubject,
                ownerSubject: ownerSubject,
                scope:
                [
                    new ApiKeyScopeDto
                    {
                        Role = UserRole.TeamAdmin,
                        ScopeType = PermissionScopeType.Team,
                        ScopeId = TeamId,
                    },
                ]);

            return creationResult.PlainTextKey;
        }

        private WebApplicationFactory<Program> WithRealRbacService(TestWebApplicationFactory<Program> root)
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
                        defaultOptions.DefaultScheme = SecurityPolicyScheme;
                        defaultOptions.DefaultAuthenticateScheme = SecurityPolicyScheme;
                        defaultOptions.DefaultChallengeScheme = SecurityPolicyScheme;
                        defaultOptions.DefaultForbidScheme = SecurityPolicyScheme;
                    })
                    .AddPolicyScheme(SecurityPolicyScheme, "Delete Unit Rights Policy Scheme", options =>
                    {
                        options.ForwardDefaultSelector = context =>
                            context.Request.Headers.ContainsKey("X-Api-Key")
                                ? ApiKeyScheme
                                : TestAuthHandler.SchemeName;
                        options.ForwardChallenge = TestAuthHandler.SchemeName;
                        options.ForwardForbid = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        _ => { })
                    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                        ApiKeyScheme,
                        _ => { });

                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseService.Object);
                });
            });
        }
    }
}
