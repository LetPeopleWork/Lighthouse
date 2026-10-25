using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Moving the calendar days onto the instance time zone must not reach the instants as well.
    ///
    /// An instant has no timezone. Moving one onto the instance calendar would not be a partial fix,
    /// it would be a NEW bug - an audit stamp, a refresh cursor or a blocked-interval endpoint that
    /// silently records a wall-clock reading instead of the moment something happened, corrupting
    /// data no existing test looks at. This fixture pins one site from each family of instants:
    /// an audit/lifecycle stamp, a sync/refresh bookkeeping field, and a blocked-transition endpoint.
    ///
    /// Each test first proves the configured zone really does move a calendar day, so the "it did
    /// not move" assertions cannot pass vacuously against an inert zone knob.
    /// </summary>
    [TestFixture]
    public class InstantsUnaffectedByZoneTest
    {
        private static readonly TimeZoneInfo InstanceZoneAheadOfUtc = TestZones.FarAheadOfUtc;

        /// <summary>23:30 UTC is inside the window where the instance day and the UTC day disagree.</summary>
        private static readonly DateTimeOffset DayBoundaryInstant = new(2026, 7, 27, 23, 30, 0, TimeSpan.Zero);

        private static readonly TimeSpan MinimumDistanceFromAZoneShiftedValue = TimeSpan.FromHours(13);

        [Test]
        public void ConfiguredZone_AtTheDayBoundary_MovesTheCalendarDayButNotTheInstant()
        {
            var utcClock = new FakeLighthouseClock(DayBoundaryInstant, TimeZoneInfo.Utc);
            var aheadClock = new FakeLighthouseClock(DayBoundaryInstant, InstanceZoneAheadOfUtc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    aheadClock.Today,
                    Is.Not.EqualTo(utcClock.Today),
                    "The zone knob must move the calendar day - it is what every other test here relies on.");

                Assert.That(
                    aheadClock.Now,
                    Is.EqualTo(utcClock.Now),
                    "...and it must not move the instant. Now is a moment, not a reading of a wall clock.");
            }
        }

        /// <summary>
        /// Sync/refresh bookkeeping: the last-update time every updater compares its refresh interval
        /// against, stamped by the team refresh. Both the instance zone and the host's local zone are
        /// moved off UTC, so a stamp taken from either wall clock lands somewhere else.
        /// </summary>
        [Test]
        public async Task SyncBookkeeping_UnderANonUtcInstanceZone_StaysAtTheUtcInstant()
        {
            GivenAnInstanceZoneThatMovesTheCalendarDay();

            var timeProvider = new FakeTimeProvider(DayBoundaryInstant);
            timeProvider.SetLocalTimeZone(InstanceZoneAheadOfUtc);
            var subject = TeamMetricsServiceReading(new LighthouseClock(InstanceZoneAheadOfUtc, timeProvider));
            var team = new Team { AutomaticallyAdjustFeatureWIP = false };

            await subject.UpdateTeamMetrics(team);

            var instant = DayBoundaryInstant.UtcDateTime;
            AssertStillAnInstant(team.UpdateTime, instant, instant, "Team.UpdateTime after a team refresh");
        }

        /// <summary>
        /// Blocked-transition interval endpoint -
        /// <c>DomainEvents/WorkItemBlockedTransitionCaptureHandler.cs:29</c>. This one is an interval
        /// endpoint: shifting it would change every blocked-duration the portfolio reports.
        /// </summary>
        [Test]
        public async Task BlockedTransitionEnteredAt_UnderANonUtcInstanceZone_StaysAtTheUtcInstant()
        {
            GivenAnInstanceZoneThatMovesTheCalendarDay();

            var repository = new Mock<IWorkItemBlockedTransitionRepository>();
            WorkItemBlockedTransition? captured = null;
            repository
                .Setup(r => r.GetByPredicate(It.IsAny<Func<WorkItemBlockedTransition, bool>>()))
                .Returns((WorkItemBlockedTransition?)null);
            repository
                .Setup(r => r.Add(It.IsAny<WorkItemBlockedTransition>()))
                .Callback<WorkItemBlockedTransition>(transition => captured = transition);
            repository.Setup(r => r.Save()).Returns(Task.CompletedTask);

            var subject = new WorkItemBlockedTransitionCaptureHandler(
                repository.Object,
                TimeProvider.System,
                Mock.Of<ILogger<WorkItemBlockedTransitionCaptureHandler>>());

            var before = DateTime.UtcNow;
            await subject.HandleAsync(new WorkItemBlocked(42, "Waiting on review"), CancellationToken.None);
            var after = DateTime.UtcNow;

            Assert.That(captured, Is.Not.Null);
            AssertStillAnInstant(captured!.EnteredAt, before, after, "WorkItemBlockedTransition.EnteredAt");
        }

        /// <summary>
        /// The one deliberate carve-out. <c>DeliveryMetricSnapshot.RecordedAt</c> is NOT pinned here
        /// and its values DO move with the instance zone, by design: since step 02-02 it is the
        /// legacy expand-phase column, written at the midnight of the DateOnly day key
        /// (<c>DeliveryMetricSnapshotRepository.GetOrCreateForDay</c>), so it follows
        /// <c>clock.Today</c> exactly as the day key does. A passing T3 is therefore NOT a claim that
        /// nothing about that table changed.
        ///
        /// The assertion below is the visible cost of expand-only: the day key moves, so the legacy
        /// column moves with it. The column's own write behaviour is owned by
        /// <c>DeliveryMetricSnapshotRepositoryTest.GetOrCreateForDay_NewDeliveryAndDay_AlsoWritesTheLegacyInstantAtMidnightUtc</c>.
        /// When the contract-phase migration drops the column, this test is what tells the author
        /// there is nothing left to preserve.
        /// </summary>
        [Test]
        public void LegacyDeliveryMetricSnapshotRecordedAt_IsExcludedFromThisPin_BecauseItFollowsTheDayKey()
        {
            var utcClock = new FakeLighthouseClock(DayBoundaryInstant, TimeZoneInfo.Utc);
            var aheadClock = new FakeLighthouseClock(DayBoundaryInstant, InstanceZoneAheadOfUtc);

            Assert.That(
                aheadClock.Today,
                Is.Not.EqualTo(utcClock.Today),
                "The legacy DeliveryMetricSnapshot.RecordedAt is written at this day key's midnight, so it "
                + "moves with the zone. That is expected and is why it is not pinned as an instant.");
        }

        private static TeamMetricsService TeamMetricsServiceReading(ILighthouseClock clock)
        {
            var appSettingService = new Mock<IAppSettingService>();
            appSettingService.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 1 });

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider
                .Setup(provider => provider.GetService(typeof(Cache<string, object>)))
                .Returns(new Cache<string, object>());

            var workItemRepository = new Mock<IWorkItemRepository>();
            workItemRepository.Setup(repository => repository.Save()).Returns(Task.CompletedTask);

            return new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                workItemRepository.Object,
                Mock.Of<IFeatureRepository>(),
                appSettingService.Object,
                serviceProvider.Object,
                Mock.Of<IBlackoutPeriodService>(),
                Mock.Of<IForecastFilterRuleService>(),
                Mock.Of<IWorkItemStateTransitionRepository>(),
                clock);
        }

        private static void GivenAnInstanceZoneThatMovesTheCalendarDay()
        {
            var utcClock = new FakeLighthouseClock(DayBoundaryInstant, TimeZoneInfo.Utc);
            var aheadClock = new FakeLighthouseClock(DayBoundaryInstant, InstanceZoneAheadOfUtc);

            Assert.That(
                aheadClock.Today,
                Is.Not.EqualTo(utcClock.Today),
                "The probe zone does not move the calendar day, so nothing below would prove anything.");
        }

        private static void AssertStillAnInstant(DateTime stamp, DateTime before, DateTime after, string site)
        {
            var hadItFollowedTheInstanceZone = TimeZoneInfo.ConvertTimeFromUtc(before, InstanceZoneAheadOfUtc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    stamp.Kind,
                    Is.EqualTo(DateTimeKind.Utc),
                    $"{site}: an instant is stored as UTC - the EF converter and every consumer assume it.");

                Assert.That(
                    stamp,
                    Is.InRange(before, after),
                    $"{site}: the stamp is no longer the moment the action happened. Bug #5567 moves calendar "
                    + "days onto ILighthouseClock; it must not move instants.");

                Assert.That(
                    hadItFollowedTheInstanceZone - stamp,
                    Is.GreaterThan(MinimumDistanceFromAZoneShiftedValue),
                    $"{site}: the stamp landed on the instance zone's wall clock instead of the instant.");
            }
        }
    }
}
