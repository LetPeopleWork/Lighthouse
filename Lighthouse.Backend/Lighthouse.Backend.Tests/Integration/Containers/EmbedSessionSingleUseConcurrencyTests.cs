using System.Net;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    // Epic 5146 slice 02a (#5641) — ADR-131; feature security checklist S4.
    // EF InMemory cannot model the conditional update this property rests on, so the probe needs a real provider.
    [TestFixture]
    [Category("requires-docker")]
    public class EmbedSessionSingleUseConcurrencyTests
    {
        private const string OwnerSubject = "embed-concurrency-owner";
        private const int ConcurrentRedemptions = 8;

        [Test]
        public async Task Redeem_ManySimultaneousRedemptionsOfOneToken_ExactlyOneEstablishesASession()
        {
            await using var postgres = await PostgresContainerFixture.StartFreshAsync();
            using var factory = new PostgresEmbedFactory(postgres.GetConnectionString());

            MigrateAndSeedOwner(factory);
            var apiKey = await CreateApiKeyAsync(factory);
            var token = await EmbedSessionTestHost.MintTokenAsync(factory, apiKey);

            using var barrier = new Barrier(ConcurrentRedemptions);
            var redemptions = Enumerable.Range(0, ConcurrentRedemptions)
                .Select(_ => Task.Run(async () =>
                {
                    barrier.SignalAndWait();
                    using var response = await EmbedSessionTestHost.EnterAsync(factory, token);
                    return response.StatusCode;
                }))
                .ToList();

            var statuses = await Task.WhenAll(redemptions);

            var successes = statuses.Count(status => status == HttpStatusCode.Redirect);
            var refusals = statuses.Count(status => status == HttpStatusCode.Unauthorized);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(successes, Is.EqualTo(1),
                    "single use is the conditional update's affected-row count; a read-then-write passes every "
                    + "single-threaded test and hands out extra sessions exactly when it matters");
                Assert.That(refusals, Is.EqualTo(ConcurrentRedemptions - 1),
                    "every loser of the race must be refused legibly, not served a second session");
            }
        }

        private static void MigrateAndSeedOwner(PostgresEmbedFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.Migrate();

            var profile = new UserProfile
            {
                Subject = OwnerSubject,
                SubjectClaimType = "sub",
                DisplayName = "Embed Concurrency Owner",
                Email = "embed-concurrency@example.test",
            };
            dbContext.UserProfiles.Add(profile);
            dbContext.SaveChanges();

            dbContext.UserPermissions.Add(new UserPermission
            {
                UserProfileId = profile.Id,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            dbContext.SaveChanges();
        }

        private static async Task<string> CreateApiKeyAsync(PostgresEmbedFactory factory)
        {
            using var scope = factory.Services.CreateScope();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<IApiKeyService>();
            var creationResult = await apiKeyService.CreateApiKeyAsync(
                "embed-concurrency-key",
                "epic 5146 slice 02a",
                createdByUser: OwnerSubject,
                ownerSubject: OwnerSubject);

            return creationResult.PlainTextKey;
        }

        private sealed class PostgresEmbedFactory : WebApplicationFactory<Program>
        {
            private readonly string connectionString;

            public PostgresEmbedFactory(string connectionString)
            {
                this.connectionString = connectionString;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");

                // Provider is chosen from configuration, so the app registers Npgsql itself —
                // registering a second DbContext here would put two providers in one container.
                builder.UseSetting("Database:Provider", "postgres");
                builder.UseSetting("Database:ConnectionString", connectionString);
                builder.UseSetting("Authentication:Enabled", "true");
                builder.UseSetting("Authentication:Authority", "https://example.test/oidc");
                builder.UseSetting("Authentication:ClientId", "lighthouse-embed-test");
                builder.UseSetting("Authentication:ClientSecret", "test-secret");
                builder.UseSetting("Authentication:MetadataAddress", "https://example.test/oidc/.well-known/openid-configuration");
                builder.UseSetting("Authentication:RequireHttpsMetadata", "false");
                builder.UseSetting("Authentication:AllowedOrigins:0", "https://lighthouse.test");
                builder.UseSetting("Authorization:Enabled", "true");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IHostedService>();
                    services.AddSingleton<IStartupFilter>(new UnservedSpaPageStartupFilter());

                    var licenseServiceMock = new Mock<ILicenseService>();
                    licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
                    services.RemoveAll<ILicenseService>();
                    services.AddScoped(_ => licenseServiceMock.Object);
                });
            }
        }
    }
}
