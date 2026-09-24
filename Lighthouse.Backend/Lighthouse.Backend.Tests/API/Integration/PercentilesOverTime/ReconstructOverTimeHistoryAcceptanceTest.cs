using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.BackgroundServices;
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
        /// The period of the last trend a scenario opened. A second replica standing in for another copy
        /// of the application is a replica whose user opened the same chart, so it walks the same days -
        /// give it a different span and the two walks run sixty days apart and never meet, which looks
        /// like a race and is not one.
        /// </summary>
        private (DateOnly From, DateOnly To)? periodLastOpened;

        /// <summary>
        /// The team the current scenario's sized deliveries are broken down for, once one has been
        /// needed. Cleared per scenario alongside the database it lives in: the fixture is one object
        /// for the whole class, so an id kept from the previous scenario would name a row that the
        /// next scenario's fresh database does not have.
        /// </summary>
        private int? teamTheDeliveriesAreBrokenDownFor;

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
            fillPause = new FillPause();
            stagedDayPause = new StagedDayPause();
            teamTheDeliveriesAreBrokenDownFor = null;

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
        /// Where the stretch this owner's limits are drawn from begins, and how long the owner keeps
        /// finished work for. The two together decide whether that stretch is still within reach, and
        /// from which day the reach is measured is the question this slice exists to settle. Read back
        /// off the owner rather than restated by the caller, so a scenario that asserts a relation
        /// between the two cannot drift away from what was actually seeded.
        /// </summary>
        protected (DateOnly StretchStartsOn, int DaysFinishedWorkIsKeptFor) HowTheTeamsStretchAndRetentionStand(int teamId)
        {
            using var scope = Factory.Services.CreateScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;

            return StretchAndRetention(team.ProcessBehaviourChartBaselineStartDate, team.DoneItemsCutoffDays);
        }

        protected (DateOnly StretchStartsOn, int DaysFinishedWorkIsKeptFor) HowThePortfoliosStretchAndRetentionStand(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!;

            return StretchAndRetention(portfolio.ProcessBehaviourChartBaselineStartDate, portfolio.DoneItemsCutoffDays);
        }

        private static (DateOnly StretchStartsOn, int DaysFinishedWorkIsKeptFor) StretchAndRetention(DateTime? stretchStartsOn, int daysFinishedWorkIsKeptFor)
        {
            Assert.That(stretchStartsOn, Is.Not.Null,
                "No stretch is pinned on this owner, so there is no reach to judge and the arrangement the caller is about to " +
                "assert never happened.");

            return (DateOnly.FromDateTime(stretchStartsOn!.Value), daysFinishedWorkIsKeptFor);
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
            => SeedFinishedDelivery(portfolioId, referenceId, startedOn, closedOn, brokenDownIntoItems: null);

        /// <summary>
        /// A delivery this portfolio finished, of a stated size. How big a delivery is, is how much
        /// work it was broken down into for the teams doing it - so a delivery with no breakdown has a
        /// size of zero, and a portfolio whose deliveries all have one genuinely has no size process to
        /// report. Scenarios that read "how big are deliveries getting" have to seed through here; the
        /// plain seeder above leaves that question with no honest answer.
        /// </summary>
        protected void SeedSizedDeliveryFinishedOn(int portfolioId, string referenceId, DateOnly startedOn, DateOnly closedOn, int brokenDownIntoItems)
            => SeedFinishedDelivery(portfolioId, referenceId, startedOn, closedOn, brokenDownIntoItems);

        /// <summary>
        /// However many sized deliveries the portfolio finished on each day of a span, in a single
        /// write. Same reason as the batched item seeder above: a varying daily count needs hundreds of
        /// rows, and a commit each would be most of the scenario's runtime.
        /// </summary>
        protected void SeedSizedDeliveriesFinishedOn(
            int portfolioId, DateOnly from, DateOnly to, Func<DateOnly, int> howManyFinishedOn, Func<DateOnly, int> howBigEachWas)
        {
            var doneByTeamId = TheTeamTheDeliveriesAreBrokenDownFor();

            using var scope = Factory.Services.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!;
            var doneBy = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(doneByTeamId)!;
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

            for (var day = from; day <= to; day = day.AddDays(1))
            {
                var finishedThatDay = howManyFinishedOn(day);

                for (var nth = 0; nth < finishedThatDay; nth++)
                {
                    repository.Add(FinishedDelivery(
                        portfolio, doneBy, $"{portfolioId}-{day:yyyyMMdd}-{nth}", day.AddDays(-4), day, howBigEachWas(day)));
                }
            }

            repository.Save().GetAwaiter().GetResult();
        }

        private void SeedFinishedDelivery(int portfolioId, string referenceId, DateOnly startedOn, DateOnly closedOn, int? brokenDownIntoItems)
        {
            var doneByTeamId = brokenDownIntoItems.HasValue ? TheTeamTheDeliveriesAreBrokenDownFor() : (int?)null;

            using var scope = Factory.Services.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!;
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

            var doneBy = doneByTeamId.HasValue
                ? scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(doneByTeamId.Value)!
                : null;

            var delivery = FinishedDelivery(portfolio, doneBy, referenceId, startedOn, closedOn, brokenDownIntoItems);

            repository.Add(delivery);
            repository.Save().GetAwaiter().GetResult();
        }

        private static Feature FinishedDelivery(
            Portfolio portfolio, Team? doneBy, string referenceId, DateOnly startedOn, DateOnly closedOn, int? brokenDownIntoItems)
        {
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

            if (doneBy != null)
            {
                // Nothing left to do: the delivery is finished. Only the total is what its size is read
                // from, and a delivery still carrying remaining work would additionally need a forecast
                // before the rest of the product would treat it as answerable.
                delivery.AddOrUpdateWorkForTeam(doneBy, 0, brokenDownIntoItems!.Value);
            }

            return delivery;
        }

        /// <summary>
        /// The team a sized delivery's work is broken down for. One team for the whole scenario, made
        /// on first use, because which team does the work never matters to anything asked here - only
        /// that a breakdown needs one to belong to.
        ///
        /// No chart is ever opened for it, so no reconstruction pass is ever queued for it and it holds
        /// no over-time rows of its own. A scenario that asserts a team reports no delivery sizes is
        /// asserting that about the team it actually read, not about this one.
        /// </summary>
        private int TheTeamTheDeliveriesAreBrokenDownFor()
        {
            teamTheDeliveriesAreBrokenDownFor ??= SeedTeamObservedUntil(TodayDay);

            return teamTheDeliveriesAreBrokenDownFor.Value;
        }

        /// <summary>
        /// A delivery that has started and not finished, which is what a portfolio needs before any day
        /// has an age to report: age is a reading of what was running on the day itself, and a delivery
        /// that closed on that day was no longer running by it.
        /// </summary>
        protected void SeedDeliveryStillInProgressSince(int portfolioId, string referenceId, DateOnly startedOn)
        {
            using var scope = Factory.Services.CreateScope();
            var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(portfolioId)!;
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();

            var delivery = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Delivery {referenceId}",
                Type = "Epic",
                State = DoingState,
                StateCategory = StateCategories.Doing,
                CreatedDate = startedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                StartedDate = startedOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                ClosedDate = null,
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
            RememberThePeriodOpened(from, to);
            Client.AsTeamAdmin(teamId);
            var query = $"?metricType={metricType}{HorizonPart(horizon)}{RangePart(from, to)}";
            return await Read($"/api/latest/teams/{teamId}/metrics/percentiles-over-time{query}");
        }

        protected async Task<SeriesResponse> ReadPortfolioPercentileTrend(int portfolioId, MetricType metricType, int? horizon, DateOnly? from = null, DateOnly? to = null)
        {
            RememberThePeriodOpened(from, to);
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
        /// Waits for everything the reads above asked for to be written, so a scenario can state what
        /// the chart holds afterwards without sleeping. Resolving the filler from the host is also how
        /// a scenario proves the registration shape: the test host removes every hosted service, so a
        /// filler registered only as one would not be here to ask.
        /// </summary>
        protected Task TheReconstructionPassRunsToCompletion()
            => ThisReplicasFiller.DrainAsync(CancellationToken.None);

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

            var heldPass = Task.Run(() => filler.DrainAsync(CancellationToken.None));

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

        /// <summary>
        /// Runs one pass to the end while another writer takes one of its days out from under it. The
        /// pass is held at the moment it has worked a day out and has not yet committed it,
        /// <paramref name="takeTheDay"/> writes that same day from a connection of its own, and only
        /// then is the pass let go - so its insert meets a row that was not there when it looked. The
        /// day it was holding is handed back, because that is the day the other writer was told to take.
        ///
        /// Holding the pass where <see cref="AReconstructionPassHeldInFlight"/> holds it would not do:
        /// there the pass has not yet looked to see whether the day is missing, so it would find the
        /// other writer's row, step over it, and the refusal this exists to produce would never happen.
        ///
        /// One pass, not two. Two replicas walking the same window cannot show this at all - whatever
        /// one abandons the other writes, so the window comes out whole either way and a pass that gives
        /// up on its first refusal is indistinguishable from one that carries on.
        /// </summary>
        protected async Task<DateOnly> TheReconstructionPassRunsWhileAnotherWriterTakesADayFromUnderIt(Action<DateOnly> takeTheDay)
        {
            var filler = ThisReplicasFiller;
            stagedDayPause.Arm();

            var pass = Task.Run(() => filler.DrainAsync(CancellationToken.None));

            var dayBeingCommitted = stagedDayPause.WaitUntilADayIsWorkedOutButNotCommitted(LongEnoughThatSomethingIsWrong);

            Assert.That(dayBeingCommitted, Is.Not.Null,
                "No pass reached the point of committing a day, so there is nothing for another writer to " +
                "take. Either nothing was queued for this owner or the pass finished before it could be " +
                "caught - and a scenario that then watches a pass meet a day someone else took is watching " +
                "neither of those things happen.");

            takeTheDay(dayBeingCommitted!.Value);

            stagedDayPause.Release();

            var finished = await Task.WhenAny(pass, Task.Delay(LongEnoughThatSomethingIsWrong));

            Assert.That(finished, Is.SameAs(pass),
                $"The pass never finished after {dayBeingCommitted:yyyy-MM-dd} was taken from under it.");

            await pass;

            return dayBeingCommitted.Value;
        }

        /// <summary>
        /// Runs two passes over the same owner at the same instant, one per replica.
        ///
        /// The second replica is a second filler instance rather than a second ask on this one, and the
        /// distinction is the whole scenario: one instance keeps a set of the owners it is already
        /// working on and collapses a second ask into the first, so a single instance cannot produce two
        /// simultaneous passes for one owner however it is asked. That set is a within-process
        /// optimisation. Across processes there is no such set, and the unique natural key is the only
        /// thing standing between two replicas and two points on the same date.
        ///
        /// This replica's pass is the one the chart load queued; the other stands in for a copy of the
        /// application whose own chart load happened somewhere this test host cannot see.
        /// </summary>
        protected async Task TwoReconstructionPassesRunAtTheSameInstant(int ownerId, OwnerType ownerType)
        {
            var thisReplica = ThisReplicasFiller;

            using var otherReplica = new OverTimeHistoryFiller(
                Factory.Services.GetRequiredService<IServiceScopeFactory>(),
                Factory.Services.GetRequiredService<ILogger<OverTimeHistoryFiller>>());

            otherReplica.AskFor(new OverTimeFillRequest(
                ownerId, ownerType, DaysCarryingNoReadingYet(ownerId, ownerType, MetricType.CycleTime)));

            await Task.WhenAll(
                Task.Run(() => thisReplica.DrainAsync(CancellationToken.None)),
                Task.Run(() => otherReplica.DrainAsync(CancellationToken.None)));
        }

        /// <summary>
        /// Runs one pass, on a budget, over the same days the chart the scenario just opened is missing.
        /// A filler of its own rather than the registered one, because the budget a shipped pass runs to
        /// is measured in seconds a test cannot afford to spend waiting - and a budget nothing in the
        /// suite can reach is one nobody can show is enforced at all.
        ///
        /// Everything else about the pass is the shipped one: the same walk, the same writer, the same
        /// database. Only how long it is allowed to keep going differs.
        /// </summary>
        protected async Task AReconstructionPassRunsOnABudgetOf(
            TimeSpan budget, int ownerId, OwnerType ownerType, MetricType metricType)
        {
            using var passOnABudget = new OverTimeHistoryFiller(
                Factory.Services.GetRequiredService<IServiceScopeFactory>(),
                Factory.Services.GetRequiredService<ILogger<OverTimeHistoryFiller>>(),
                budget);

            passOnABudget.AskFor(new OverTimeFillRequest(
                ownerId, ownerType, DaysCarryingNoReadingYet(ownerId, ownerType, metricType)));

            await passOnABudget.DrainAsync(CancellationToken.None);
        }

        private OverTimeHistoryFiller ThisReplicasFiller
            => Factory.Services.GetRequiredService<OverTimeHistoryFiller>();

        private void RememberThePeriodOpened(DateOnly? from, DateOnly? to)
        {
            if (from.HasValue && to.HasValue)
            {
                periodLastOpened = (from.Value, to.Value);
            }
        }

        /// <summary>
        /// What a replica opening the same chart would find missing: the same period this scenario just
        /// opened, minus whatever already carries a reading. Same period on purpose - two replicas walking
        /// different spans pass each other rather than contend, and the scenario would then be watching
        /// two passes that never touch the same day.
        /// </summary>
        private List<DateOnly> DaysCarryingNoReadingYet(int ownerId, OwnerType ownerType, MetricType metricType)
        {
            var period = periodLastOpened ?? (From: TodayDay.AddDays(1 - ReconstructionCapInDays), To: TodayDay);

            using var scope = Factory.Services.CreateScope();
            var held = scope.ServiceProvider.GetRequiredService<IPercentilesOverTimeSnapshotRepository>()
                .GetAll()
                .Where(snapshot => snapshot.OwnerId == ownerId && snapshot.OwnerType == ownerType && snapshot.MetricType == metricType)
                .Select(snapshot => snapshot.RecordedAt)
                .ToHashSet();

            var missing = new List<DateOnly>();
            for (var day = period.From; day <= period.To && missing.Count < ReconstructionCapInDays; day = day.AddDays(1))
            {
                if (!held.Contains(day))
                {
                    missing.Add(day);
                }
            }

            return missing;
        }

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

        // --- What a day reads when worked out some other way ---

        /// <summary>
        /// What the throughput limits read when worked out over the stretch from <paramref name="firstDay"/>
        /// to <paramref name="lastDay"/>, judged as of <paramref name="judgedAsOf"/>. It asks the product's
        /// own calculation directly rather than going through a chart load, because a scenario that claims
        /// to catch a wrong way of working a day out first has to know that the wrong way gives a different
        /// answer. A stretch judged unusable reads as all zeros, which no written day ever carries.
        ///
        /// The team's metrics cache is emptied afterwards. Otherwise the reading asked for here would be
        /// served straight back to the reconstruction pass that follows, and the scenario would be holding
        /// the recorder up against this helper rather than against reconstruction.
        /// </summary>
        protected (int Unpl, int Average, int Lnpl) ThroughputLimitsWorkedOutOver(int teamId, DateOnly firstDay, DateOnly lastDay, DateOnly judgedAsOf)
        {
            using var scope = Factory.Services.CreateScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
            var metrics = scope.ServiceProvider.GetRequiredService<ITeamMetricsService>();

            var chart = metrics.GetThroughputProcessBehaviourChart(
                team, InstanceCalendar.AsUtcMidnight(firstDay), InstanceCalendar.AsUtcMidnight(lastDay), judgedAsOf);
            metrics.InvalidateTeamMetrics(team);

            return (chart.UpperNaturalProcessLimit, chart.Average, chart.LowerNaturalProcessLimit);
        }

        /// <summary>
        /// The cycle-time percentiles of what the team finished from <paramref name="firstDay"/> to
        /// <paramref name="lastDay"/>, from the product's own calculation. Same purpose, and the same
        /// reason for emptying the cache afterwards, as the throughput reading above.
        /// </summary>
        protected (int P50, int P70, int P85, int P95) CycleTimePercentilesWorkedOutOver(int teamId, DateOnly firstDay, DateOnly lastDay)
        {
            using var scope = Factory.Services.CreateScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
            var metrics = scope.ServiceProvider.GetRequiredService<ITeamMetricsService>();

            var percentiles = metrics.GetCycleTimePercentilesForTeam(
                team, InstanceCalendar.AsUtcMidnight(firstDay), InstanceCalendar.AsUtcMidnight(lastDay)).ToList();
            metrics.InvalidateTeamMetrics(team);

            int At(int percentile) => percentiles.SingleOrDefault(value => value.Percentile == percentile)?.Value ?? 0;

            return (At(50), At(70), At(85), At(95));
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
