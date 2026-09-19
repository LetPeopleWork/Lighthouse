using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.SleRisk
{
    /// <summary>
    /// Epic-wide harness for #4127 — the chance an in-flight item has of missing the target its team
    /// published. Slices 02-04 inherit it rather than standing up a host each.
    ///
    /// The instance clock is pinned. An in-flight item's age is counted against whatever day the
    /// instance believes it is, so without a fixed instant the ages drift with the calendar and the
    /// expected numbers in every scenario go stale overnight rather than at a boundary anyone chose.
    /// </summary>
    public abstract class SleRiskAcceptanceTest
    {
        protected const string InProgress = "In Progress";

        /// <summary>
        /// Every seeded date is expressed as a number of days before this. The value itself is
        /// arbitrary; that it never moves is the point.
        /// </summary>
        protected static readonly DateTimeOffset Today =
            new(2026, 9, 16, 9, 0, 0, TimeSpan.Zero);

        private TestWebApplicationFactory<Program> rootFactory = null!;

        private FakeLighthouseClock fakeClock = null!;

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        /// <summary>
        /// The window every scenario asks about, and the window every seeded item is dated inside
        /// unless the scenario is specifically about falling outside it.
        /// </summary>
        protected DateTime WindowStart => Today.UtcDateTime.Date.AddDays(-180);

        protected DateTime WindowEnd => Today.UtcDateTime.Date;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            var authenticated = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);

            Factory = authenticated.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    fakeClock = new FakeLighthouseClock(Today);
                    services.RemoveAll<ILighthouseClock>();
                    services.AddSingleton<ILighthouseClock>(fakeClock);

                    // The read itself carries no licence check, and deliberately so. Write-back does,
                    // and a scenario that compares the two surfaces against each other has to be able
                    // to reach both — otherwise it silently compares the screens against nothing.
                    var license = new Mock<ILicenseService>();
                    license.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
                    services.RemoveAll<ILicenseService>();
                    services.AddSingleton(license.Object);
                });
            });

            Client = Factory.CreateClient();

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = Factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            rootFactory.Dispose();
        }

        /// <summary>
        /// A team whose published target is <paramref name="rangeInDays"/> days, or no target at all
        /// when that is zero. The probability half of the pair is stored because the settings screen
        /// stores it; nothing on this path reads it.
        /// </summary>
        protected int SeedTeamWithTarget(int rangeInDays, int probability = 80)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                DoneItemsCutoffDays = 0,
                WorkItemTypes = ["Story"],
                ToDoStates = ["New"],
                DoingStates = [InProgress],
                DoneStates = ["Done"],
                ServiceLevelExpectationRange = rangeInDays,
                ServiceLevelExpectationProbability = rangeInDays > 0 ? probability : 0,
            };

            var repository = sp.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        /// <summary>
        /// The team publishes a different target than the one it started with — one click in the
        /// settings, and half of every answer this route gives.
        /// </summary>
        protected void ChangeTheTargetOf(int teamId, int rangeInDays)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = repository.GetById(teamId)!;
            team.ServiceLevelExpectationRange = rangeInDays;

            repository.Update(team);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// The team pins which stretch of finished work it counts, rather than rolling with the
        /// calendar. The window then stays put while the days keep passing — which is the only
        /// arrangement in which an answer dated to today can be told apart from one dated to the
        /// end of the evidence.
        /// </summary>
        protected void PinTheHistoryOf(int teamId, int startDaysAgo, int endDaysAgo)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = repository.GetById(teamId)!;
            team.UseFixedDatesForThroughput = true;
            team.ThroughputHistoryStartDate = Today.UtcDateTime.Date.AddDays(-startDaysAgo);
            team.ThroughputHistoryEndDate = Today.UtcDateTime.Date.AddDays(-endDaysAgo);

            repository.Update(team);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>The instance believes a day has passed. Nothing in the database moves.</summary>
        protected void AdvanceTheInstanceToTomorrow()
            => fakeClock.SetInstant(Today.AddDays(1));

        /// <summary>
        /// The team counts throughput over a different stretch of time — the other setting the answer
        /// depends on, and one that moves without the target moving.
        /// </summary>
        protected void ChangeTheHistoryOf(int teamId, int historyInDays)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = repository.GetById(teamId)!;
            team.ThroughputHistory = historyInDays;

            repository.Update(team);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// A finished item that took exactly <paramref name="cycleTimeInDays"/> days, closed inside
        /// the window unless <paramref name="closedDaysBeforeWindowStart"/> puts it outside.
        /// </summary>
        protected void SeedFinishedItem(int teamId, int cycleTimeInDays, int closedDaysBeforeWindowStart = 0)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            // Well inside the window rather than up against its edge, so a scenario that asks about a
            // window ending in the past still finds this work in it.
            var closed = closedDaysBeforeWindowStart > 0
                ? WindowStart.AddDays(-closedDaysBeforeWindowStart)
                : WindowEnd.AddDays(-20);

            // Both ends are counted, so a cycle time of n days spans n-1 days of calendar.
            var started = closed.AddDays(-(cycleTimeInDays - 1));

            var repository = sp.GetRequiredService<IWorkItemRepository>();
            var reference = $"DONE-{Guid.NewGuid():N}";

            repository.Add(new WorkItem
            {
                TeamId = teamId,
                Team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId)!,
                ReferenceId = reference,
                Name = $"Finished in {cycleTimeInDays} days",
                Type = "Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                CreatedDate = started.AddDays(-1),
                StartedDate = started,
                ClosedDate = closed,
                Order = reference,
            });

            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>Several finished items in one call, so a scenario reads as its distribution.</summary>
        protected void SeedFinishedItems(int teamId, params int[] cycleTimesInDays)
        {
            foreach (var cycleTime in cycleTimesInDays)
            {
                SeedFinishedItem(teamId, cycleTime);
            }
        }

        /// <summary>An item still open, which the instance's pinned today makes exactly this old.</summary>
        protected string SeedInFlightItem(int teamId, int ageInDays)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            // Age counts both ends too: an item started today is one day old.
            var started = WindowEnd.AddDays(-(ageInDays - 1));
            var reference = $"WIP-{Guid.NewGuid():N}";

            var repository = sp.GetRequiredService<IWorkItemRepository>();
            repository.Add(new WorkItem
            {
                TeamId = teamId,
                Team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId)!,
                ReferenceId = reference,
                Name = $"Open for {ageInDays} days",
                Type = "Story",
                State = InProgress,
                StateCategory = StateCategories.Doing,
                CreatedDate = started.AddDays(-1),
                StartedDate = started,
                ClosedDate = null,
                Order = reference,
            });

            repository.Save().GetAwaiter().GetResult();

            return reference;
        }

        /// <summary>
        /// No dates. The route takes none, because the window is the team's own configured history
        /// and the question is about today — neither of which a caller gets to choose.
        /// </summary>
        protected static Uri SleRiskRoute(int teamId) => new(
            $"/api/latest/teams/{teamId}/metrics/sleRisk",
            UriKind.Relative);
    }
}
