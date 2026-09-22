using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Story-wide acceptance harness for 6053 - an over-time chart shows the trend the stored work items
    /// already support, instead of the handful of days the recorder happened to catch.
    ///
    /// Separate from the Epic 5427 harness on purpose. These scenarios turn on which calendar day the
    /// instance believes it is - how far back the walk may reach, which days sit after an owner's last
    /// observation, and where the cap falls - so the clock is pinned. The epic-5427 harness leaves the
    /// clock real, and pinning it there would move every one of its expectations.
    ///
    /// The app is the production app: the real ASP.NET host over a real SQLite FILE (not the in-memory
    /// provider), real EF, real DI. That matters beyond style here - the in-memory provider does not
    /// enforce unique indexes, so the natural-key collision that the concurrency story rests on is
    /// invisible to it and would pass against an implementation that has no backstop at all. Only the
    /// licence port and the clock are substituted, both being external or non-deterministic.
    /// </summary>
    public abstract class ReconstructOverTimeHistoryAcceptanceTest
    {
        /// <summary>
        /// The most days one background pass may write. Locked at ninety: a pass that stops there is
        /// resumable on the next read, so a year-wide picker fills over successive loads rather than in
        /// one unbounded walk.
        /// </summary>
        protected const int ReconstructionCapInDays = 90;

        protected const string DoingState = "In Progress";

        protected const string DoneState = "Done";

        /// <summary>
        /// Every seeded date is expressed as a number of days before this instant. The value is
        /// arbitrary; that it never moves is the point, because a walk-back boundary measured against a
        /// drifting "today" goes stale overnight rather than at a boundary anyone chose.
        /// </summary>
        protected static readonly DateTimeOffset Today = new(2026, 9, 22, 9, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// The seam these scenarios need and the product does not have yet. Spelled out rather than left
        /// as a bare failure, because the shape of the seam is the handoff: the pass must be awaitable
        /// from a test, or every "the gap fills" scenario has to sleep and every "nothing was written"
        /// scenario passes for the wrong reason.
        /// </summary>
        private const string MissingFillerSeam =
            "The over-time history filler does not exist yet. It must be registered as a singleton that " +
            "survives the test host's removal of hosted services (register the concrete type as a " +
            "singleton, then add the hosted service as a factory over it), and it must expose a drain " +
            "that processes everything already queued and returns when the queue is empty, plus a " +
            "pass-in-flight predicate the database maintenance gate can consult.";

        private TestWebApplicationFactory<Program> rootFactory = null!;

        private FakeLighthouseClock instanceClock = null!;

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        /// <summary>The calendar day the instance believes it is, for every scenario in this story.</summary>
        protected static DateOnly TodayDay => DateOnly.FromDateTime(Today.UtcDateTime);

        protected DatabaseMaintenanceGate MaintenanceGate =>
            Factory.Services.GetRequiredService<DatabaseMaintenanceGate>();

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            Factory = TestWebApplicationFactory<Program>
                .WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        instanceClock = new FakeLighthouseClock(Today);
                        services.RemoveAll<ILighthouseClock>();
                        services.AddSingleton<ILighthouseClock>(instanceClock);

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
        /// Moves the day the instance believes it is. Used by the fidelity scenario, which has to let the
        /// recorder write a day for real before asking reconstruction to produce the same day again.
        /// </summary>
        protected void TheInstanceMovesOnTo(DateOnly day)
            => instanceClock.SetInstant(new DateTimeOffset(day.ToDateTime(new TimeOnly(9, 0)), TimeSpan.Zero));

        /// <summary>
        /// Removes a recorded day, standing in for the instance having been switched off when the
        /// recorder would otherwise have run. It is the arrangement half of the fidelity scenario: the
        /// value the recorder produced is read out first, then the day is forgotten, so what
        /// reconstruction later writes can be compared against what was genuinely observed.
        /// </summary>
        protected void TheRecordOfThatPercentileDayIsLost(int ownerId, OwnerType ownerType, MetricType metricType, int horizon, DateOnly day)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>();
            var recorded = repository.GetByPredicate(s =>
                s.OwnerId == ownerId && s.OwnerType == ownerType && s.MetricType == metricType && s.Horizon == horizon && s.RecordedAt == day);

            Assert.That(recorded, Is.Not.Null, $"There is no recorded {metricType} day on {day:yyyy-MM-dd} to forget - the arrangement never happened.");

            repository.Remove(recorded);
            repository.Save().GetAwaiter().GetResult();
        }

        protected void TheRecordOfThatLimitDayIsLost(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType family, DateOnly day)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>();
            var recorded = repository.GetByPredicate(s =>
                s.OwnerId == ownerId && s.OwnerType == ownerType && s.MetricType == family && s.RecordedAt == day);

            Assert.That(recorded, Is.Not.Null, $"There is no recorded {family} day on {day:yyyy-MM-dd} to forget - the arrangement never happened.");

            repository.Remove(recorded);
            repository.Save().GetAwaiter().GetResult();
        }

        // --- Seeding: preconditions only, never the expected output ---

        /// <summary>
        /// A team last seen by its tracker on <paramref name="lastObservedOn"/>. That day is the floor
        /// under which nothing may be reconstructed: past it the team's items are frozen at the break,
        /// so computing days beyond it would draw a confident trend for a period nobody observed.
        /// </summary>
        protected int SeedTeamObservedUntil(DateOnly lastObservedOn, int doneItemsCutoffDays = 365, int throughputHistory = 30)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                DoneItemsCutoffDays = doneItemsCutoffDays,
                ThroughputHistory = throughputHistory,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = [DoingState],
                DoneStates = [DoneState],
                UpdateTime = lastObservedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            };

            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        protected int SeedPortfolioObservedUntil(DateOnly lastObservedOn, int doneItemsCutoffDays = 365)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                DoneItemsCutoffDays = doneItemsCutoffDays,
                DataRetrievalValue = "project = TEST",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["New"],
                DoingStates = [DoingState],
                DoneStates = [DoneState],
                UpdateTime = lastObservedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            };

            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        protected void PinTheTeamsBaselineTo(int teamId, DateOnly from, DateOnly to)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var team = repository.GetById(teamId)!;

            team.ProcessBehaviourChartBaselineStartDate = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            team.ProcessBehaviourChartBaselineEndDate = to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            repository.Update(team);
            repository.Save().GetAwaiter().GetResult();
        }

        protected void PinThePortfoliosBaselineTo(int portfolioId, DateOnly from, DateOnly to)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            var portfolio = repository.GetById(portfolioId)!;

            portfolio.ProcessBehaviourChartBaselineStartDate = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            portfolio.ProcessBehaviourChartBaselineEndDate = to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            repository.Update(portfolio);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// One item this team finished. Dates are anchored at UTC midnight because every persisted
        /// instant goes through the UTC converter, and a local midnight would land on the previous day
        /// and move the item into a neighbouring window.
        /// </summary>
        protected void SeedItemFinishedOn(int teamId, string referenceId, DateOnly startedOn, DateOnly closedOn)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            repository.Add(new WorkItem
            {
                TeamId = teamId,
                ReferenceId = referenceId,
                Name = $"Item {referenceId}",
                Type = "Story",
                State = DoneState,
                StateCategory = StateCategories.Done,
                CreatedDate = startedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                StartedDate = startedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                ClosedDate = closedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Order = referenceId,
            });

            repository.Save().GetAwaiter().GetResult();
        }

        protected void SeedItemStillInProgressSince(int teamId, string referenceId, DateOnly startedOn)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            repository.Add(new WorkItem
            {
                TeamId = teamId,
                ReferenceId = referenceId,
                Name = $"Item {referenceId}",
                Type = "Story",
                State = DoingState,
                StateCategory = StateCategories.Doing,
                CreatedDate = startedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                StartedDate = startedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                ClosedDate = null,
                Order = referenceId,
            });

            repository.Save().GetAwaiter().GetResult();
        }

        protected void SeedDeliveryFinishedOn(int portfolioId, string referenceId, DateOnly startedOn, DateOnly closedOn)
        {
            using var scope = Factory.Services.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!;
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

            var delivery = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Delivery {referenceId}",
                Type = "Epic",
                State = DoneState,
                StateCategory = StateCategories.Done,
                CreatedDate = startedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                StartedDate = startedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                ClosedDate = closedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Order = referenceId,
            };
            delivery.Portfolios.Add(portfolio);

            repository.Add(delivery);
            repository.Save().GetAwaiter().GetResult();
        }

        protected void SeedRecordedPercentileDay(int ownerId, OwnerType ownerType, MetricType metricType, int horizon, DateOnly recordedOn, int p50, int p70, int p85, int p95)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>();

            repository.Add(new PercentilesOverTimeSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                MetricType = metricType,
                Horizon = horizon,
                RecordedAt = recordedOn,
                P50 = p50,
                P70 = p70,
                P85 = p85,
                P95 = p95,
            });

            repository.Save().GetAwaiter().GetResult();
        }

        protected void SeedRecordedLimitDay(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType family, DateOnly recordedOn, int unpl, int average, int lnpl)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>();

            repository.Add(new ProcessBehaviorSnapshot
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                MetricType = family,
                RecordedAt = recordedOn,
                Unpl = unpl,
                Average = average,
                Lnpl = lnpl,
            });

            repository.Save().GetAwaiter().GetResult();
        }

        // --- Driving ports: the two shipped read endpoints, over real HTTP ---

        protected async Task<SeriesResponse> ReadTeamPercentileTrend(int teamId, MetricType metricType, int? horizon, DateOnly? from = null, DateOnly? to = null)
        {
            Client.AsTeamAdmin(teamId);
            var query = $"?metricType={metricType}{HorizonPart(horizon)}{RangePart(from, to)}";
            return await Read($"/api/latest/teams/{teamId}/metrics/percentiles-over-time{query}");
        }

        protected async Task<SeriesResponse> ReadPortfolioPercentileTrend(int portfolioId, MetricType metricType, int? horizon, DateOnly? from = null, DateOnly? to = null)
        {
            Client.AsPortfolioAdmin(portfolioId);
            var query = $"?metricType={metricType}{HorizonPart(horizon)}{RangePart(from, to)}";
            return await Read($"/api/latest/portfolios/{portfolioId}/metrics/percentiles-over-time{query}");
        }

        protected async Task<SeriesResponse> ReadTeamLimitTrend(int teamId, ProcessBehaviorMetricType family, DateOnly? from = null, DateOnly? to = null)
        {
            Client.AsTeamAdmin(teamId);
            return await Read($"/api/latest/teams/{teamId}/metrics/process-behavior-over-time?type={family}{RangePart(from, to)}");
        }

        protected async Task<SeriesResponse> ReadPortfolioLimitTrend(int portfolioId, ProcessBehaviorMetricType family, DateOnly? from = null, DateOnly? to = null)
        {
            Client.AsPortfolioAdmin(portfolioId);
            return await Read($"/api/latest/portfolios/{portfolioId}/metrics/process-behavior-over-time?type={family}{RangePart(from, to)}");
        }

        /// <summary>The shipped write path: recording runs off the refresh event, not off a read.</summary>
        protected async Task TheTeamsRefreshCompletes(int teamId)
        {
            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>().PublishAsync(new TeamDataRefreshed(teamId));
        }

        protected async Task ThePortfoliosRefreshCompletes(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>().PublishAsync(new PortfolioFeaturesRefreshed(portfolioId));
        }

        // --- The background pass ---

        /// <summary>
        /// SCAFFOLD: waits for everything the reads above asked for to be written, so a scenario can
        /// state what the chart holds afterwards without sleeping. Fails until the filler exists - which
        /// is deliberate: without it, every scenario whose outcome is "no row was written" would pass
        /// against a product in which nothing writes rows at all.
        /// </summary>
        protected static Task TheReconstructionPassRunsToCompletion()
            => throw new AssertionException(MissingFillerSeam);

        /// <summary>
        /// SCAFFOLD: holds a pass open so a scenario can observe the system while one is running, and
        /// releases it when disposed.
        /// </summary>
        protected static IDisposable AReconstructionPassHeldInFlight()
            => throw new AssertionException(MissingFillerSeam);

        /// <summary>
        /// SCAFFOLD: runs two passes over the same owner at the same instant, from two scopes, the way
        /// two replicas of the application would. Both writing the same day must leave one row, not two,
        /// and neither may fail the pass.
        /// </summary>
        protected static Task TwoReconstructionPassesRunAtTheSameInstant(int ownerId, OwnerType ownerType)
            => throw new AssertionException(MissingFillerSeam);

        // --- Observing what the chart holds ---

        protected IReadOnlyList<RecordedPercentileDay> PercentileDaysHeldFor(int ownerId, OwnerType ownerType, MetricType metricType, int horizon)
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>()
                .GetAll()
                .Where(s => s.OwnerId == ownerId && s.OwnerType == ownerType && s.MetricType == metricType && s.Horizon == horizon)
                .OrderBy(s => s.RecordedAt)
                .Select(s => new RecordedPercentileDay(s.RecordedAt, s.P50, s.P70, s.P85, s.P95))];
        }

        protected IReadOnlyList<RecordedLimitDay> LimitDaysHeldFor(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType family)
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>()
                .GetAll()
                .Where(s => s.OwnerId == ownerId && s.OwnerType == ownerType && s.MetricType == family)
                .OrderBy(s => s.RecordedAt)
                .Select(s => new RecordedLimitDay(s.RecordedAt, s.Unpl, s.Average, s.Lnpl))];
        }

        protected IReadOnlyList<ProcessBehaviorMetricType> LimitFamiliesHeldFor(int ownerId, OwnerType ownerType)
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>()
                .GetAll()
                .Where(s => s.OwnerId == ownerId && s.OwnerType == ownerType)
                .Select(s => s.MetricType)
                .Distinct()
                .OrderBy(family => family)];
        }

        protected int TotalPercentileDaysHeld()
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>().GetAll().Count();
        }

        protected int TotalLimitDaysHeld()
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IProcessBehaviorSnapshotRepository>().GetAll().Count();
        }

        // --- Reading a series response ---

        protected static IReadOnlyList<DateOnly> DatesIn(SeriesResponse response)
        {
            Assert.That(response.Body.TrimStart(), Does.StartWith("["),
                $"An over-time series read must answer with a dated series, gap or no gap. " +
                $"Status: {response.Status}. Body starts: {Excerpt(response.Body)}");

            using var document = JsonDocument.Parse(response.Body);
            return [.. document.RootElement
                .EnumerateArray()
                .Select(element => DateOnly.Parse(element.GetProperty("recordedAt").GetString()!))];
        }

        protected static string Excerpt(string body) => body[..Math.Min(80, body.Length)];

        protected readonly record struct RecordedPercentileDay(DateOnly RecordedAt, int P50, int P70, int P85, int P95);

        protected readonly record struct RecordedLimitDay(DateOnly RecordedAt, int Unpl, int Average, int Lnpl);

        protected readonly record struct SeriesResponse(HttpStatusCode Status, string Body);

        private async Task<SeriesResponse> Read(string route)
        {
            var response = await Client.GetAsync(route);
            return new SeriesResponse(response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private static string HorizonPart(int? horizon) => horizon.HasValue ? $"&horizon={horizon.Value}" : string.Empty;

        private static string RangePart(DateOnly? from, DateOnly? to)
        {
            var range = from.HasValue ? $"&startDate={from.Value:yyyy-MM-dd}" : string.Empty;
            return to.HasValue ? $"{range}&endDate={to.Value:yyyy-MM-dd}" : range;
        }
    }
}
