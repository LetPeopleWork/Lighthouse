using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for Slice 10 - days worked out afterwards read only work the team still keeps.
    ///
    /// The team here is refreshed today and keeps finished work for ninety days, so the oldest day it
    /// still holds finished work from is ninety days back. The store below still holds older items,
    /// because nothing in this fixture runs a sync that would delete them - which is exactly what makes
    /// the edge observable: a fill that ignored it would find those items and write a reading from them.
    /// </summary>
    public partial class Slice10OnlyWhatTheTeamStillKeepsTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        private const int DaysTheTeamKeepsFinishedWorkFor = 90;

        private const int ThirtyDays = 30;

        /// <summary>
        /// How far back a thirty-day cycle time reading looks: the items finished on the day itself and
        /// on each of the thirty days before it.
        /// </summary>
        private const int DaysACycleTimeReadingReachesBack = ThirtyDays;

        /// <summary>
        /// How far back a day's limits look when no stretch is pinned: the team's thirty days of
        /// throughput history, the day itself included.
        /// </summary>
        private const int DaysTheLimitsReachBack = 29;

        private static DateOnly TheOldestDayTheTeamStillKeeps => TodayDay.AddDays(-DaysTheTeamKeepsFinishedWorkFor);

        // --- Given ---

        private int GivenATeamStillBeingRefreshedThatKeepsFinishedWorkFor(int days)
            => SeedTeamObservedUntil(TodayDay, doneItemsCutoffDays: days);

        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
            => SeedItemsFinishedOn(teamId, from, to, _ => 1);

        private void GivenTheTeamPinnedTheStretchItsLimitsAreDrawnFrom(int teamId, DateOnly from, DateOnly to)
            => PinTheTeamsBaselineTo(teamId, from, to);

        /// <summary>
        /// What makes the pinned-stretch scenario able to fail, checked through the product's own
        /// calculation: judged as of the oldest day opened - the one that reaches furthest back - the
        /// stretch still gives limits. Were it refused there already, every day would come back empty
        /// whether or not the team's last refresh is consulted, and the scenario would prove nothing.
        /// </summary>
        private void GivenThatStretchStillReadsAsOfTheOldestDayOpened(int teamId, DateOnly oldestDayOpened)
        {
            var judgedAsOfThatDay = ThroughputLimitsWorkedOutOver(
                teamId, oldestDayOpened.AddDays(-DaysTheLimitsReachBack), oldestDayOpened, oldestDayOpened);

            Assert.That(judgedAsOfThatDay, Is.Not.EqualTo((0, 0, 0)),
                $"Judged as of {oldestDayOpened:yyyy-MM-dd} the pinned stretch already gives no limits, so an empty chart " +
                "would follow whichever day the product judged it by.");
        }

        private void GivenAnItemTheTeamHasHadInProgressSince(int teamId, DateOnly startedOn)
            => SeedItemStillInProgressSince(teamId, $"{teamId}-in-progress", startedOn);

        // --- When ---

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheThirtyDayCycleTimeTrend(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.CycleTime, ThirtyDays, from, to);

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheWorkItemAgeTrend(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.WorkItemAge, horizon: null, from, to);

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensTheThroughputLimits(int teamId, DateOnly from, DateOnly to)
            => ReadTeamLimitTrend(teamId, ProcessBehaviorMetricType.Throughput, from, to);

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        // --- Then ---

        private void ThenNoCycleTimeDayReadsFurtherBackThanTheTeamKeepsWork(int teamId)
        {
            var readingPastTheEdge = CycleTimeDaysHeldFor(teamId)
                .Where(day => day.AddDays(-DaysACycleTimeReadingReachesBack) < TheOldestDayTheTeamStillKeeps)
                .ToList();

            Assert.That(readingPastTheEdge, Is.Empty,
                $"The team keeps finished work back to {TheOldestDayTheTeamStillKeeps:yyyy-MM-dd} and no further, so a thirty-day " +
                "reading on these days averages over work a refresh has already deleted, and would stay written forever.");
        }

        private void ThenEveryCycleTimeDayReadingOnlyKeptWorkWasFilledIn(int teamId, DateOnly lastDayOpened)
        {
            var missing = DaysFrom(TheOldestDayTheTeamStillKeeps.AddDays(DaysACycleTimeReadingReachesBack), lastDayOpened)
                .Except(CycleTimeDaysHeldFor(teamId))
                .ToList();

            Assert.That(missing, Is.Empty,
                "Every thirty-day window on these days lies inside what the team keeps, so each of them has a reading to fill in.");
        }

        private void ThenNoLimitDayReadsFurtherBackThanTheTeamKeepsWork(int teamId)
        {
            var readingPastTheEdge = LimitDaysHeldFor(teamId)
                .Where(day => day.AddDays(-DaysTheLimitsReachBack) < TheOldestDayTheTeamStillKeeps)
                .ToList();

            Assert.That(readingPastTheEdge, Is.Empty,
                $"The team keeps finished work back to {TheOldestDayTheTeamStillKeeps:yyyy-MM-dd} and no further, so limits drawn " +
                "on these days come from a stretch that is partly deleted, and read lower than the team ever was.");
        }

        private void ThenEveryLimitDayReadingOnlyKeptWorkWasFilledIn(int teamId, DateOnly lastDayOpened)
        {
            var missing = DaysFrom(TheOldestDayTheTeamStillKeeps.AddDays(DaysTheLimitsReachBack), lastDayOpened)
                .Except(LimitDaysHeldFor(teamId))
                .ToList();

            Assert.That(missing, Is.Empty,
                "Every stretch the limits are drawn from on these days lies inside what the team keeps, so each of them has limits to fill in.");
        }

        private void ThenNoLimitDayWasFilledInBetween(int teamId, DateOnly from, DateOnly to)
        {
            var written = LimitDaysHeldFor(teamId)
                .Where(day => day >= from && day <= to)
                .ToList();

            Assert.That(written, Is.Empty,
                $"The stretch these limits are drawn from starts before {TheOldestDayTheTeamStillKeeps:yyyy-MM-dd}, the oldest day " +
                "the team still keeps finished work from, so it is partly deleted and the limits drawn from it read lower than the " +
                $"team ever was. Written anyway: {string.Join(", ", written)}.");
        }

        private void ThenEveryLimitDayWasFilledInBetween(int teamId, DateOnly from, DateOnly to)
        {
            var missing = DaysFrom(from, to)
                .Except(LimitDaysHeldFor(teamId))
                .ToList();

            Assert.That(missing, Is.Empty,
                "This team's stretch lies wholly inside what it keeps, so every day opened has limits to fill in. " +
                $"Missing: {string.Join(", ", missing)}.");
        }

        private void ThenNoAgeDayIsOlderThanTheWorkTheTeamKeeps(int teamId)
        {
            var olderThanTheEdge = AgeDaysHeldFor(teamId)
                .Where(day => day < TheOldestDayTheTeamStillKeeps)
                .ToList();

            Assert.That(olderThanTheEdge, Is.Empty,
                $"The team keeps finished work back to {TheOldestDayTheTeamStillKeeps:yyyy-MM-dd} and no further, so an age reading " +
                "before that misses every item that was in progress then and has since finished and been deleted.");
        }

        private void ThenEveryAgeDayFromTheOldestKeptDayOnWasFilledIn(int teamId, DateOnly lastDayOpened)
        {
            var missing = DaysFrom(TheOldestDayTheTeamStillKeeps, lastDayOpened)
                .Except(AgeDaysHeldFor(teamId))
                .ToList();

            Assert.That(missing, Is.Empty,
                "The team has had an item in progress throughout, and from the oldest day it keeps onwards each day has an age to fill in.");
        }

        private List<DateOnly> CycleTimeDaysHeldFor(int teamId)
            => [.. PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays).Select(day => day.RecordedAt)];

        private List<DateOnly> AgeDaysHeldFor(int teamId)
            => [.. PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.WorkItemAge, PercentilesOverTimeSnapshot.NoHorizon).Select(day => day.RecordedAt)];

        private List<DateOnly> LimitDaysHeldFor(int teamId)
            => [.. LimitDaysHeldFor(teamId, OwnerType.Team, ProcessBehaviorMetricType.Throughput).Select(day => day.RecordedAt)];

        private static List<DateOnly> DaysFrom(DateOnly first, DateOnly last)
        {
            var days = new List<DateOnly>();
            for (var day = first; day <= last; day = day.AddDays(1))
            {
                days.Add(day);
            }

            return days;
        }
    }
}
