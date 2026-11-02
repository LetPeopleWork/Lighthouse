using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// The harness with one host for the whole fixture. Starting a host costs more than most scenarios take,
    /// so the host is built once and every scenario gets its own database, clock, licence answer, tracker,
    /// captured log and captured pushes inside it.
    ///
    /// The update queue and the status store live as long as the host, so a scenario must leave the queue
    /// idle: teardown waits for it and fails the scenario that left work behind, rather than letting it
    /// stall the next one.
    /// </summary>
    public abstract class TaskManagerHostPerFixtureAcceptanceTest : TaskManagerHarness
    {
        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            RootFactory = new TestWebApplicationFactory<Program>();
            ResetTheTestState();
            BuildTheHost();
            Factory.StartServer();
        }

        [SetUp]
        public void GiveTheScenarioItsOwnState()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            ResetTheTestState();
            RootFactory.Clock.Reset();
            RootFactory.UseFreshDatabase();
            Factory.ForgetTheCachedMetrics();
            GiveTheTestAnEmptySeededDatabase();
        }

        [TearDown]
        public async Task LeaveTheHostIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            await TheQueueGoesIdleOrFails();

            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            Factory?.Dispose();
            RootFactory?.Dispose();
        }

        /// <summary>
        /// A second configuration of the production host that shares the fixture's database, for a scenario
        /// about something the harness replaces wholesale. Call it from a [OneTimeSetUp] and dispose the result in a
        /// [OneTimeTearDown], so the host is built once and not for every scenario.
        /// </summary>
        protected WebApplicationFactory<Program> DeriveAHost(Action<IServiceCollection> configure)
        {
            return RootFactory.WithWebHostBuilder(builder => builder.ConfigureServices(configure));
        }

        /// <summary>
        /// Everything but being a system administrator is granted, so a route guarded by anything weaker
        /// would let the caller through and the scenario would say so.
        /// </summary>
        protected WebApplicationFactory<Program> DeriveAHostRefusingNonAdministrators()
            => DeriveAHost(services =>
            {
                var refuses = new Mock<IRbacAdministrationService>();
                refuses
                    .Setup(s => s.CanSatisfyRequirementAsync(
                        It.IsAny<ClaimsPrincipal>(),
                        It.IsAny<RbacGuardRequirement>(),
                        It.IsAny<int?>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(false);

                services.RemoveAll<IRbacAdministrationService>();
                services.AddScoped(_ => refuses.Object);
            });
    }
}
