using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers.McpInboundAuth
{
    public sealed class McpInboundAuthTestHost : IDisposable
    {
        private const string ApiKeyHeaderName = "X-Api-Key";

        private readonly TestWebApplicationFactory<Program> rootFactory;
        private readonly WebApplicationFactory<Program> factory;
        private readonly HttpClient client;

        public McpInboundAuthTestHost()
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
                builder.UseSetting("Authentication:AllowedOrigins:0", "https://lighthouse.test");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseService.Object);
                });
            });

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        public async Task SeedSystemAdminAsync(string subject)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var profile = new UserProfile { Subject = subject, SubjectClaimType = "sub", DisplayName = "System Admin" };
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync();

            db.UserPermissions.Add(new UserPermission
            {
                UserProfileId = profile.Id,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            await db.SaveChangesAsync();
        }

        public async Task<int> SeedTeamAsync(string name)
        {
            using var scope = factory.Services.CreateScope();
            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = new Team
            {
                Name = name,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Connection",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                },
            };
            teamRepository.Add(team);
            await teamRepository.Save();

            return team.Id;
        }

        public Task<string> SeedCallerAsync(
            string subject,
            IReadOnlyList<int> ownerTeamIds,
            IReadOnlyList<int>? keyScopeTeamIds)
        {
            var ownerPermissions = ownerTeamIds.Select(TeamAdminOn).ToList();
            var keyScope = keyScopeTeamIds?.Select(TeamAdminScope).ToList();
            return PersistCallerAsync(subject, ownerPermissions, keyScope);
        }

        public Task<string> SeedSystemAdminCallerAsync(string subject, IReadOnlyList<int> keyScopeTeamIds)
        {
            var ownerPermissions = new List<UserPermission>
            {
                new() { Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System, ScopeId = null },
            };
            var keyScope = keyScopeTeamIds.Select(TeamAdminScope).ToList();
            return PersistCallerAsync(subject, ownerPermissions, keyScope);
        }

        public Task<string> SeedCallerWithSystemScopedKeyAsync(string subject, int ownerTeamId)
        {
            var ownerPermissions = new List<UserPermission> { TeamAdminOn(ownerTeamId) };
            var keyScope = new List<ApiKeyScopeDto>
            {
                new() { Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System, ScopeId = null },
            };
            return PersistCallerAsync(subject, ownerPermissions, keyScope);
        }

        private static UserPermission TeamAdminOn(int teamId) => new()
        {
            Role = UserRole.TeamAdmin,
            ScopeType = PermissionScopeType.Team,
            ScopeId = teamId,
        };

        private static ApiKeyScopeDto TeamAdminScope(int teamId) => new()
        {
            Role = UserRole.TeamAdmin,
            ScopeType = PermissionScopeType.Team,
            ScopeId = teamId,
        };

        private async Task<string> PersistCallerAsync(
            string subject,
            List<UserPermission> ownerPermissions,
            List<ApiKeyScopeDto>? keyScope)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var profile = new UserProfile { Subject = subject, SubjectClaimType = "sub", DisplayName = subject };
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync();

            foreach (var permission in ownerPermissions)
            {
                permission.UserProfileId = profile.Id;
                db.UserPermissions.Add(permission);
            }
            await db.SaveChangesAsync();

            var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var creation = await apiKeyService.CreateApiKeyAsync(
                $"key-{subject}", "mcp-caller", createdByUser: subject, ownerSubject: subject, scope: keyScope);

            return creation.PlainTextKey;
        }

        public async Task<HttpStatusCode> GetTeamAsync(int teamId, string apiKey)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/latest/teams/{teamId}");
            request.Headers.Add(ApiKeyHeaderName, apiKey);

            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        public async Task<HttpStatusCode> UpdateTeamDataAsync(int teamId, string apiKey)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/latest/teams/{teamId}");
            request.Headers.Add(ApiKeyHeaderName, apiKey);

            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        public void Dispose()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }
    }
}
