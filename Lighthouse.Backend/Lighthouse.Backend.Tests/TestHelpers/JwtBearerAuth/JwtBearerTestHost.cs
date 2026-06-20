using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers.JwtBearerAuth
{
    public sealed record JwtTokenOptions
    {
        public required string Subject { get; init; }
        public string Audience { get; init; } = JwtBearerTestHost.Audience;
        public bool Expired { get; init; }
        public bool ValidSignature { get; init; } = true;
    }

    public sealed class JwtBearerTestHost : IDisposable
    {
        public const string Issuer = "https://idp.example.test";
        public const string Audience = "lighthouse-api-test";

        private readonly RSA signingRsa = RSA.Create(2048);
        private readonly RSA foreignRsa = RSA.Create(2048);
        private readonly TestWebApplicationFactory<Program> rootFactory;
        private readonly WebApplicationFactory<Program> factory;
        private readonly HttpClient client;

        public JwtBearerTestHost()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            var trustedSigningKey = new RsaSecurityKey(signingRsa);

            factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Enabled", "true");
                builder.UseSetting("Authentication:Authority", Issuer);
                builder.UseSetting("Authentication:Audience", Audience);
                builder.UseSetting("Authentication:ClientId", "lighthouse-test");
                builder.UseSetting("Authentication:ClientSecret", "test-secret");
                builder.UseSetting(
                    "Authentication:MetadataAddress",
                    $"{Issuer}/.well-known/openid-configuration");
                builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
                builder.UseSetting("Authorization:Enabled", "true");
                builder.UseSetting("Authentication:AllowedOrigins:0", "https://lighthouse.test");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseService.Object);

                    services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(
                        new JwtBearerTestConfiguration(trustedSigningKey, Issuer, Audience));
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

        public async Task SeedTeamAdminUserAsync(string subject, int teamId)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var profile = new UserProfile { Subject = subject, SubjectClaimType = "sub", DisplayName = subject };
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync();

            db.UserPermissions.Add(new UserPermission
            {
                UserProfileId = profile.Id,
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = teamId,
            });
            await db.SaveChangesAsync();
        }

        public string MintToken(JwtTokenOptions options)
        {
            var key = new RsaSecurityKey(options.ValidSignature ? signingRsa : foreignRsa);
            var now = DateTime.UtcNow;

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = options.Audience,
                IssuedAt = now.AddMinutes(-1),
                NotBefore = options.Expired ? now.AddMinutes(-10) : now.AddMinutes(-1),
                Expires = options.Expired ? now.AddMinutes(-5) : now.AddMinutes(30),
                Claims = new Dictionary<string, object>
                {
                    ["sub"] = options.Subject,
                    ["name"] = options.Subject,
                },
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
            };

            return new JsonWebTokenHandler().CreateToken(descriptor);
        }

        public async Task<HttpStatusCode> GetTeamWithBearerAsync(int teamId, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/latest/teams/{teamId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        public void Dispose()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
            signingRsa.Dispose();
            foreignRsa.Dispose();
        }
    }
}
