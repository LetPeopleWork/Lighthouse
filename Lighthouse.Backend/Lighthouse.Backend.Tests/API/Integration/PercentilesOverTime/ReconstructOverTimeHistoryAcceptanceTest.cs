using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
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
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Acceptance harness for rebuilding over-time history: an over-time chart shows the trend the
    /// stored work items already support, instead of the handful of days the recorder happened to catch.
    ///
    /// Separate from the percentiles-over-time recorder's harness on purpose. These scenarios turn on
    /// which calendar day the instance believes it is - how far back the walk may reach, which days sit
    /// after an owner's last observation, and where the cap falls - so the clock is pinned. The
    /// recorder's harness leaves the clock real, and pinning it there would move every one of its
    /// expectations.
    ///
    /// The app is the production app: the real ASP.NET host over a real SQLite FILE (not the in-memory
    /// provider), real EF, real DI. That matters beyond style here - the in-memory provider does not
    /// enforce unique indexes, so the natural-key collision that the concurrency story rests on is
    /// invisible to it and would pass against an implementation that has no backstop at all. Only the
    /// licence port and the clock are substituted, both being external or non-deterministic.
    ///
    /// One host serves the whole fixture, and every scenario gets its own database, clock and latches
    /// inside it. The filler's queue and the memo of what earlier passes found out live as long as the
    /// host, and both are keyed by owner id while every fresh database numbers its owners from one
    /// again - so a scenario ends by running out the filler's queue and making the memo forget each owner
    /// it seeded, and fails if the host is not left idle.
    /// </summary>
    public abstract class ReconstructOverTimeHistoryAcceptanceTest : IntegrationFixture
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
        /// How long a scenario waits for a pass to reach the point of writing, and how long a held pass
        /// has to finish once released. Generous on purpose: this is a deadlock guard, not a timing
        /// assertion, and a value tight enough to be interesting would fail on a loaded runner.
        /// </summary>
        private static readonly TimeSpan LongEnoughThatSomethingIsWrong = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The wall-clock budget a pass runs to in this host. Comfortably past the longest a scenario can
        /// hold one parked, so the only thing that ever stops a pass here is the thing the scenario is
        /// about.
        /// </summary>
        private static readonly TimeSpan LongerThanAnyScenarioHoldsAPass = TimeSpan.FromMinutes(5);

        private TestWebApplicationFactory<Program> rootFactory = null!;

        private FakeLighthouseClock instanceClock = null!;

        private FillPause fillPause = null!;

        private StagedDayPause stagedDayPause = null!;

        /// <summary>
        /// Every owner the current scenario seeded. The memo keeps notes per owner id for as long as the
        /// host lives, so these are the ids it has to be told to forget when the scenario is over.
        /// </summary>
        private readonly List<(int OwnerId, OwnerType OwnerType)> ownersSeeded = [];

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        /// <summary>The calendar day the instance believes it is, for every scenario in this story.</summary>
        protected static DateOnly TodayDay => DateOnly.FromDateTime(Today.UtcDateTime);

        [OneTimeSetUp]
        public void StartTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeSetUp);

            rootFactory = new TestWebApplicationFactory<Program>();
            fillPause = new FillPause();
            stagedDayPause = new StagedDayPause();

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

                        // A pass here is given far longer than the shipped budget allows, for the same
                        // reason the clock above is pinned: scenarios below park a pass on a latch and
                        // hold it there while they arrange something on another connection, and how long
                        // that takes is a property of the machine the suite runs on. Left at the shipped
                        // value, a loaded runner would hand a held pass a budget it had already spent
                        // while parked, and the scenario would watch it stand down over the wait rather
                        // than over the thing it was arranging. That the budget is obeyed at all is
                        // pinned by the scenario that hands a pass a budget of its own.
                        services.RemoveAll<OverTimeHistoryFiller>();
                        services.AddSingleton(provider => new OverTimeHistoryFiller(
                            provider.GetRequiredService<IServiceScopeFactory>(),
                            provider.GetRequiredService<ILogger<OverTimeHistoryFiller>>(),
                            LongerThanAnyScenarioHoldsAPass));

                        // The real writer, behind two latches a scenario can close - one at the point a
                        // day is worked out, one at the point it is committed. Nothing is intercepted
                        // until a scenario arms one, so every other scenario runs the shipped write path
                        // unchanged.
                        services.RemoveAll<IPercentileSnapshotWriter>();
                        services.AddScoped<IPercentileSnapshotWriter>(provider => new PausableFillWriter(
                            ActivatorUtilities.CreateInstance<PercentileSnapshotWriter>(provider), fillPause, stagedDayPause));

                        ConfigureAdditionalServices(services);
                    });
                });

            Client = Factory.CreateClient();

        }

        [SetUp]
        public void GiveTheScenarioItsOwnState()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.SetUp);

            ownersSeeded.Clear();

            rootFactory.Clock.Reset();
            instanceClock.SetInstant(Today);
            rootFactory.UseFreshDatabase();
            Factory.ForgetTheCachedMetrics();

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
        public async Task LeaveTheHostIdle()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.TearDown);

            fillPause.Release();
            stagedDayPause.Release();

            try
            {
                await SharedHostCleanup.DrainTheHistoryFillerOrFail(ThisReplicasFiller, LongerThanAnyScenarioHoldsAPass);
                await SharedHostCleanup.WaitUntilTheQueueIsIdleOrFail(Factory.Services, TimeSpan.FromSeconds(30));
                SharedHostCleanup.ReopenTheMaintenanceGate(Factory.Services);
            }
            finally
            {
                // A failed wait above must not leave the memo's notes or the database behind for the next
                // scenario on this host; the failure itself still propagates.
                await SharedHostCleanup.ForgetOwners(Factory.Services, ownersSeeded);

                using var scope = Factory.Services.CreateScope();
                await scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeletedAsync();
            }
        }

        [OneTimeTearDown]
        public void StopTheHost()
        {
            using var _ = FixtureSetupTimer.Measure(GetType().Name, FixtureSetupTimer.MeasurementKind.OneTimeTearDown);

            Client?.Dispose();
            Factory?.Dispose();
            rootFactory?.Dispose();
        }

        /// <summary>
        /// Room for a slice to add to the host before it is built - a way of watching the log or the
        /// database that only its own scenarios need. Everything else about the host stays as above.
        /// </summary>
        protected virtual void ConfigureAdditionalServices(IServiceCollection services)
        {
            // Nothing beyond the shared host: most slices observe only what the charts hold.
        }

        // --- Upgrades ---

        /// <summary>What an upgrade is, as far as a stored setting can tell: every seeder runs again.</summary>
        protected void TheInstanceIsUpgraded()
        {
            using var scope = Factory.Services.CreateScope();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
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

            ownersSeeded.Add((team.Id, OwnerType.Team));

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

            ownersSeeded.Add((portfolio.Id, OwnerType.Portfolio));

            return portfolio.Id;
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

            repository.Add(FinishedItem(teamId, referenceId, startedOn, closedOn));

            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// However many items the team finished on each day of a span, in a single write. The seeder
        /// above commits per item, which is right for the handful a scenario usually names; a scenario
        /// that needs a throughput which varies day to day needs a thousand or so, and a commit each
        /// would be most of what it spends its time on.
        /// </summary>
        protected void SeedItemsFinishedOn(int teamId, DateOnly from, DateOnly to, Func<DateOnly, int> howManyFinishedOn)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                var finishedThatDay = howManyFinishedOn(day);

                for (var nth = 0; nth < finishedThatDay; nth++)
                {
                    repository.Add(FinishedItem(teamId, $"{teamId}-{day:yyyyMMdd}-{nth}", day.AddDays(-1), day));
                }
            }

            repository.Save().GetAwaiter().GetResult();
        }

        private static WorkItem FinishedItem(int teamId, string referenceId, DateOnly startedOn, DateOnly closedOn)
        {
            return new WorkItem
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
            };
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

        /// <summary>A delivery this portfolio finished, of no particular size.</summary>
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

        // --- Driving ports: the two shipped read endpoints, over real HTTP ---

        protected async Task<SeriesResponse> ReadTeamPercentileTrend(int teamId, MetricType metricType, int? horizon, DateOnly? from = null, DateOnly? to = null)
        {
            Client.AsTeamAdmin(teamId);
            var query = $"?metricType={metricType}{HorizonPart(horizon)}{RangePart(from, to)}";
            return await Read($"/api/latest/teams/{teamId}/metrics/percentiles-over-time{query}");
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

        // --- The background pass ---

        /// <summary>
        /// Waits for everything the reads above asked for to be written, so a scenario can state what
        /// the chart holds afterwards without sleeping. Resolving the filler from the host is also how
        /// a scenario proves the registration shape: the test host removes every hosted service, so a
        /// filler registered only as one would not be here to ask.
        /// </summary>
        protected Task TheReconstructionPassRunsToCompletion()
            => ThisReplicasFiller.DrainAsync(CancellationToken.None);

        // A pass that is held blocks its thread until the test releases it. On a pool thread that parks a
        // worker, and a small pool (CI) can then starve the very test waiting to release it.
        private static Task OnADedicatedThread(Func<Task> pass)
        {
            return Task.Factory.StartNew(
                pass,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }

        /// <summary>
        /// Holds a pass genuinely open: a real pass is started, it reaches the point at which it is
        /// about to write its first day, and it waits there until this handle is disposed. Both the
        /// latch closing and the filler reporting a pass in flight are asserted before the scenario is
        /// allowed to continue, because a handle that quietly held nothing would make every scenario
        /// that observes the system "while a pass runs" pass without observing anything.
        ///
        /// It parks before the pass has staged anything, so the pass holds no database lock while it
        /// waits - a scenario is free to write from another connection in the meantime, which is the
        /// whole point of holding it.
        /// </summary>
        protected IDisposable AReconstructionPassHeldInFlight()
        {
            var filler = ThisReplicasFiller;
            fillPause.Arm();

            var heldPass = OnADedicatedThread(() => filler.DrainAsync(CancellationToken.None));

            var parked = fillPause.WaitUntilAPassIsParked(LongEnoughThatSomethingIsWrong);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parked, Is.True,
                    "No pass reached the point of writing a day, so nothing is being held. Either nothing " +
                    "was queued for this owner or the pass finished before it could be caught - and a " +
                    "scenario that then observes 'the system while a pass runs' is observing an idle one.");

                Assert.That(filler.HasPassInFlight, Is.True,
                    "A pass is parked mid-walk and the filler still reports none in flight. Every gate that " +
                    "consults it would let an operator swap the database file out from under an open write.");
            }

            return new HeldPass(fillPause, heldPass);
        }

        private OverTimeHistoryFiller ThisReplicasFiller
            => Factory.Services.GetRequiredService<OverTimeHistoryFiller>();

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

        protected int TotalPercentileDaysHeld()
        {
            using var scope = Factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>().GetAll().Count();
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

        /// <summary>
        /// A latch across the one point every reconstruction pass has to go through to write a day.
        /// Closed only while a scenario is holding a pass, and only the first pass to arrive is caught -
        /// a latch that caught every day would never let the walk finish.
        /// </summary>
        private sealed class FillPause
        {
            private readonly ManualResetEventSlim released = new(false);

            private readonly ManualResetEventSlim reached = new(false);

            private int closed;

            public void Arm()
            {
                released.Reset();
                reached.Reset();
                Volatile.Write(ref closed, 1);
            }

            public bool WaitUntilAPassIsParked(TimeSpan timeout) => reached.Wait(timeout);

            public void Release()
            {
                Volatile.Write(ref closed, 0);
                released.Set();
            }

            /// <summary>
            /// Opens the latch as it passes through it, so the pass that got here is the only one held
            /// and a release that arrives first leaves the walk untouched.
            /// </summary>
            public void ParkIfClosed(TimeSpan timeout)
            {
                if (Interlocked.CompareExchange(ref closed, 0, 1) != 1)
                {
                    return;
                }

                reached.Set();
                released.Wait(timeout);
            }
        }

        /// <summary>
        /// A latch across the moment a pass has worked a day out and is about to commit it. That gap is
        /// the only place another writer can take the day away: before it, the pass has not yet looked
        /// to see whether the day is missing and would simply step over the other writer's row.
        ///
        /// Closed only while a scenario is holding a pass, and only the first day to arrive is caught.
        /// The day it caught is readable, so the scenario knows which one to take.
        /// </summary>
        private sealed class StagedDayPause
        {
            private readonly ManualResetEventSlim released = new(false);

            private readonly ManualResetEventSlim reached = new(false);

            private int closed;

            private DateOnly dayHeld;

            public void Arm()
            {
                released.Reset();
                reached.Reset();
                Volatile.Write(ref closed, 1);
            }

            public DateOnly? WaitUntilADayIsWorkedOutButNotCommitted(TimeSpan timeout)
                => reached.Wait(timeout) ? dayHeld : null;

            public void Release()
            {
                Volatile.Write(ref closed, 0);
                released.Set();
            }

            public void ParkIfClosed(DateOnly day, TimeSpan timeout)
            {
                if (Interlocked.CompareExchange(ref closed, 0, 1) != 1)
                {
                    return;
                }

                dayHeld = day;
                reached.Set();
                released.Wait(timeout);
            }
        }

        /// <summary>
        /// The shipped writer with the latches in front of the gap-filling path only. Recording today
        /// goes straight through, which is what lets a scenario run a refresh against a fill that is
        /// parked.
        /// </summary>
        private sealed class PausableFillWriter : IPercentileSnapshotWriter
        {
            private readonly IPercentileSnapshotWriter shipped;

            private readonly FillPause pause;

            private readonly StagedDayPause commitPause;

            private DateOnly dayLastWorkedOut;

            public PausableFillWriter(IPercentileSnapshotWriter shipped, FillPause pause, StagedDayPause commitPause)
            {
                this.shipped = shipped;
                this.pause = pause;
                this.commitPause = commitPause;
            }

            public void RecordToday(
                int ownerId,
                OwnerType ownerType,
                MetricType metricType,
                Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles)
                => shipped.RecordToday(ownerId, ownerType, metricType, readPercentiles);

            public void FillDayIfAbsent(
                int ownerId,
                OwnerType ownerType,
                MetricType metricType,
                DateOnly day,
                Func<DateTime, DateTime, IEnumerable<PercentileValue>> readPercentiles)
            {
                pause.ParkIfClosed(LongEnoughThatSomethingIsWrong);
                dayLastWorkedOut = day;
                shipped.FillDayIfAbsent(ownerId, ownerType, metricType, day, readPercentiles);
            }

            public Task SaveFilledDay()
            {
                commitPause.ParkIfClosed(dayLastWorkedOut, LongEnoughThatSomethingIsWrong);
                return shipped.SaveFilledDay();
            }
        }

        private sealed class HeldPass : IDisposable
        {
            private readonly FillPause pause;

            private readonly Task pass;

            public HeldPass(FillPause pause, Task pass)
            {
                this.pause = pause;
                this.pass = pass;
            }

            public void Dispose()
            {
                pause.Release();

                Assert.That(pass.Wait(LongEnoughThatSomethingIsWrong), Is.True,
                    "The pass that was being held never finished after it was let go.");
            }
        }
    }
}
