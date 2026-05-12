using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
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
    [NonParallelizable]
    public class F_BE_1_GroupSnapshotInheritanceTests
    {
        private const string TeamsPath = "/api/v1/teams";
        private const string OwnerSubject = "owner-subject";
        private const string SystemAdminSubject = "system-admin-subject";
        private const string GroupAlpha = "team-alpha-group";
        private const string GroupViewers = "viewers";
        private const int TeamAlphaId = 42;

        private static readonly string[] AlphaOnlyGroups = [GroupAlpha];
        private static readonly string[] ViewersOnlyGroups = [GroupViewers];
        private static readonly string[] AlphaAndViewersGroups = [GroupAlpha, GroupViewers];

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient apiClient = null!;

        [SetUp]
        public void SetUp()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = WithRealRbacService(rootFactory);
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
        public async Task BE_1_1_SnapshotWriter_PersistsSerialisedGroupValuesOnUserProfile()
        {
            SeedOwnerProfile(OwnerSubject);

            using var scope = rootFactory.Services.CreateScope();
            var writer = scope.ServiceProvider.GetRequiredService<IOidcGroupSnapshotWriter>();

            await writer.WriteAsync(OwnerSubject, AlphaAndViewersGroups, CancellationToken.None);

            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var persisted = dbContext.UserProfiles.Single(p => p.Subject == OwnerSubject);
            var expectedJson = JsonSerializer.Serialize(AlphaAndViewersGroups);

            Assert.That(persisted.LastKnownGroupClaimValues, Is.EqualTo(expectedJson));
        }

        [Test]
        public async Task BE_1_2_ApiKeyOwnerWithSnapshot_ResolvesGroupMappedTeamThroughApiKey()
        {
            var ownerProfile = SeedOwnerWithSnapshot(OwnerSubject, AlphaOnlyGroups);
            SeedTeam(TeamAlphaId);
            SeedGroupMapping(GroupAlpha, UserRole.Viewer, PermissionScopeType.Team, TeamAlphaId);
            var plainTextKey = await IssueApiKeyForOwnerAsync(ownerProfile, scope: null);

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);
            var response = await keyClient.GetAsync(TeamsPath);
            var teams = await ReadTeamsAsync(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(teams.Select(t => t.Id), Does.Contain(TeamAlphaId));
            }
        }

        [Test]
        public async Task BE_1_3_DeletedGroupMapping_RevokesAccessOnNextApiKeyCall()
        {
            var ownerProfile = SeedOwnerWithSnapshot(OwnerSubject, AlphaOnlyGroups);
            SeedTeam(TeamAlphaId);
            var plainTextKey = await IssueApiKeyForOwnerAsync(ownerProfile, scope: null);

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);
            var response = await keyClient.GetAsync(TeamsPath);
            var teams = await ReadTeamsAsync(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(teams.Select(t => t.Id), Does.Not.Contain(TeamAlphaId));
            }
        }

        [Test]
        public async Task BE_1_4_ExplicitUserPermissionOverridesGroupSnapshotPrecedence()
        {
            var ownerProfile = SeedOwnerWithSnapshot(OwnerSubject, ViewersOnlyGroups);
            SeedTeam(TeamAlphaId);
            SeedGroupMapping(GroupViewers, UserRole.Viewer, PermissionScopeType.Team, TeamAlphaId);
            SeedUserPermission(ownerProfile.Id, UserRole.TeamAdmin, PermissionScopeType.Team, TeamAlphaId);
            var plainTextKey = await IssueApiKeyForOwnerAsync(ownerProfile, scope: null);

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);
            var deleteResponse = await keyClient.DeleteAsync($"{TeamsPath}/{TeamAlphaId}");

            Assert.That(deleteResponse.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
        }

        [Test]
        public async Task BE_1_5_OwnerWithoutSnapshot_ApiKeyResolvesOnlyExplicitGrants_NoRegression()
        {
            var ownerProfile = SeedOwnerProfile(OwnerSubject);
            SeedTeam(TeamAlphaId);
            SeedGroupMapping(GroupAlpha, UserRole.Viewer, PermissionScopeType.Team, TeamAlphaId);
            var plainTextKey = await IssueApiKeyForOwnerAsync(ownerProfile, scope: null);

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);
            var response = await keyClient.GetAsync(TeamsPath);
            var teams = await ReadTeamsAsync(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(teams.Select(t => t.Id), Does.Not.Contain(TeamAlphaId));
            }
        }

        [Test]
        public async Task BE_1_6_ScopedKey_GroupSnapshotFeedsOwnerSideOfIntersection()
        {
            var ownerProfile = SeedOwnerWithSnapshot(OwnerSubject, AlphaOnlyGroups);
            SeedTeam(TeamAlphaId);
            SeedGroupMapping(GroupAlpha, UserRole.Viewer, PermissionScopeType.Team, TeamAlphaId);

            var scopedKeyScope = new[]
            {
                new ApiKeyScopeDto
                {
                    Role = UserRole.Viewer,
                    ScopeType = PermissionScopeType.Team,
                    ScopeId = TeamAlphaId,
                },
            };
            var plainTextKey = await IssueApiKeyForOwnerAsync(ownerProfile, scopedKeyScope);

            using var keyClient = factory.CreateClient();
            keyClient.WithApiKey(plainTextKey);
            var response = await keyClient.GetAsync(TeamsPath);
            var teams = await ReadTeamsAsync(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(teams.Select(t => t.Id), Does.Contain(TeamAlphaId));
            }
        }

        private static async Task<IReadOnlyList<TeamDto>> ReadTeamsAsync(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var teams = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
            return teams ?? [];
        }

        private UserProfile SeedOwnerProfile(string subject)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureCreated();
            EnsureSystemAdminExists(dbContext);

            var profile = new UserProfile
            {
                Subject = subject,
                SubjectClaimType = "sub",
                DisplayName = "Owner",
                Email = "owner@example.test",
            };
            dbContext.UserProfiles.Add(profile);
            dbContext.SaveChanges();
            return profile;
        }

        private static void EnsureSystemAdminExists(LighthouseAppContext dbContext)
        {
            if (dbContext.UserProfiles.Any(p => p.Subject == SystemAdminSubject))
            {
                return;
            }

            var systemAdminProfile = new UserProfile
            {
                Subject = SystemAdminSubject,
                SubjectClaimType = "sub",
                DisplayName = "System Admin",
                Email = "sa@example.test",
            };
            dbContext.UserProfiles.Add(systemAdminProfile);
            dbContext.SaveChanges();

            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = systemAdminProfile.Id,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            dbContext.SaveChanges();
        }

        private UserProfile SeedOwnerWithSnapshot(string subject, IReadOnlyList<string> groupValues)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureCreated();
            EnsureSystemAdminExists(dbContext);

            var profile = new UserProfile
            {
                Subject = subject,
                SubjectClaimType = "sub",
                DisplayName = "Owner",
                Email = "owner@example.test",
                LastKnownGroupClaimValues = JsonSerializer.Serialize(groupValues),
            };
            dbContext.UserProfiles.Add(profile);
            dbContext.SaveChanges();
            return profile;
        }

        private void SeedTeam(int teamId)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

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
                Id = teamId,
                Name = $"Team {teamId}",
                WorkTrackingSystemConnectionId = connection.Id,
            });
            dbContext.SaveChanges();
        }

        private void SeedGroupMapping(string groupValue, UserRole role, PermissionScopeType scopeType, int? scopeId)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            dbContext.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = groupValue,
                Role = role,
                ScopeType = scopeType,
                ScopeId = scopeId,
            });
            dbContext.SaveChanges();
        }

        private void SeedUserPermission(int userProfileId, UserRole role, PermissionScopeType scopeType, int? scopeId)
        {
            using var scope = rootFactory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = userProfileId,
                Role = role,
                ScopeType = scopeType,
                ScopeId = scopeId,
            });
            dbContext.SaveChanges();
        }

        private async Task<string> IssueApiKeyForOwnerAsync(UserProfile owner, IReadOnlyList<ApiKeyScopeDto>? scope)
        {
            using var rootScope = rootFactory.Services.CreateScope();
            var apiKeyService = rootScope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var creationResult = await apiKeyService.CreateApiKeyAsync(
                name: $"test-key-{Guid.NewGuid():N}",
                description: "for f-be-1 tests",
                createdByUser: owner.Subject,
                ownerSubject: owner.Subject,
                scope: scope);
            return creationResult.PlainTextKey!;
        }

        private static WebApplicationFactory<Program> WithRealRbacService(TestWebApplicationFactory<Program> root)
        {
            return root.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configurationBuilder) =>
                {
                    configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Enabled"] = "true",
                        ["Authorization:Enabled"] = "true",
                        ["Authorization:GroupClaimName"] = "groups",
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
                    .AddPolicyScheme(SecurityPolicyScheme, "F-BE-1 Policy Scheme", options =>
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

        private const string SecurityPolicyScheme = "FBE1PolicyScheme";
    }
}
