using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    public class RbacGroupMappingConcurrencyTokenIntegrationTest
    {
        private TestWebApplicationFactory<Program> factory = null!;
        private WebApplicationFactory<Program> authFactory = null!;
        private HttpClient client = null!;
        private int seededMappingId;

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            factory = new TestWebApplicationFactory<Program>();
            authFactory = TestWebApplicationFactory<Program>.WithTestAuthentication(factory);
        }

        [SetUp]
        public void Init()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            factory.Clock.Reset();
            factory.UseFreshDatabase();
            factory.ForgetTheCachedMetrics();

            client = authFactory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            SeedGroupMapping();
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            try
            {
                SharedHostCleanup.ReopenTheMaintenanceGate(factory.Services);
                await SharedHostCleanup.DrainTheHistoryFillerOrFail(
                    factory.Services.GetRequiredService<OverTimeHistoryFiller>(), TimeSpan.FromSeconds(60));
                await SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(factory.Services, TimeSpan.FromSeconds(30));
            }
            finally
            {
                using (var teardownScope = factory.Services.CreateScope())
                {
                    await teardownScope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
                }

                client.Dispose();
            }
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            authFactory?.Dispose();
            factory?.Dispose();
        }

        [Test]
        public async Task StaleGroupMappingRoleEdit_ThroughRbacService_ThrowsConcurrencyException_FirstWriterRolePreserved()
        {
            Guid staleToken;
            using (var readScope = factory.Services.CreateScope())
            {
                var context = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                staleToken = context.RbacGroupMappings.Single(m => m.Id == seededMappingId).ConcurrencyToken;
            }

            using (var winnerScope = factory.Services.CreateScope())
            {
                var winnerService = winnerScope.ServiceProvider.GetRequiredService<IRbacAdministrationService>();
                var winnerResult = await winnerService.UpdateGroupMappingRoleAsync(
                    seededMappingId, UserRole.Viewer, staleToken, CancellationToken.None);
                Assert.That(winnerResult.Succeeded, Is.True, winnerResult.Message);
            }

            using var staleScope = factory.Services.CreateScope();
            var staleService = staleScope.ServiceProvider.GetRequiredService<IRbacAdministrationService>();

            Assert.That(
                async () => await staleService.UpdateGroupMappingRoleAsync(
                    seededMappingId, UserRole.TeamAdmin, staleToken, CancellationToken.None),
                Throws.InstanceOf<DbUpdateConcurrencyException>(),
                "A stale RBAC group-mapping edit must surface as a concurrency conflict through IRbacAdministrationService, never a silent last-writer-wins overwrite.");

            using var verifyScope = factory.Services.CreateScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var roleAfterConflict = verifyContext.RbacGroupMappings.Single(m => m.Id == seededMappingId).Role;
            Assert.That(roleAfterConflict, Is.EqualTo(UserRole.Viewer),
                "The first writer's role must be preserved; the stale edit must not overwrite it.");
        }

        [Test]
        public async Task UnauthorizedActor_EditingGroupMappingRole_Returns403_NotConflated_With409()
        {

            client.AsViewer();

            Guid token;
            using (var readScope = factory.Services.CreateScope())
            {
                var context = readScope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                token = context.RbacGroupMappings.Single(m => m.Id == seededMappingId).ConcurrencyToken;
            }

            var request = new RbacGroupMappingRoleUpdateRequest
            {
                Role = UserRole.Viewer.ToString(),
                ConcurrencyToken = token,
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await client.PutAsync(
                $"/api/latest/authorization/group-mappings/{seededMappingId}", content);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden),
                    "An unauthorized actor must be rejected with 403 by the authorization gate, which runs before any concurrency save.");
                Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Conflict),
                    "403 (authorization) and 409 (concurrency) must never be conflated.");
            }
        }

        private void SeedGroupMapping()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var mapping = new RbacGroupMapping
            {
                GroupValue = $"group-{Guid.NewGuid():N}",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            };

            context.RbacGroupMappings.Add(mapping);
            context.SaveChanges();

            seededMappingId = mapping.Id;
        }
    }
}
