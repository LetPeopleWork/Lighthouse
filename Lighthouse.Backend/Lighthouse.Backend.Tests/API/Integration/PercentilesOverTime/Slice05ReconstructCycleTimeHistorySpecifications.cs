using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for Slice 05 - the cycle-time trend a team's stored items already support.
    ///
    /// Everything enters through the shipped percentiles-over-time read endpoint. Nothing here calls a
    /// reconciler, a filler or a snapshot repository to make reconstruction happen: a chart load is the
    /// only thing a flow coach does, so a chart load is the only thing these scenarios do.
    ///
    /// The one place the scenarios reach past the endpoint is to read back what the chart now holds, and
    /// to wait for the background pass. Both are observations, not instructions.
    /// </summary>
    public partial class Slice05ReconstructCycleTimeHistoryTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        /// <summary>The horizon this slice works in. The other two arrive with slice 06.</summary>
        private const int ThirtyDays = 30;

        private const string RestoreOperationId = "restore-under-test";

        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        private int GivenATeamLastObservedOn(DateOnly lastObservedOn) => SeedTeamObservedUntil(lastObservedOn);

        /// <summary>
        /// One item finished on each day of the span, each taking two days. A steady trickle is what
        /// makes a day computable: the thirty days before it have to contain something that closed.
        /// </summary>
        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-{day:yyyyMMdd}", day.AddDays(-1), day);
            }
        }

        private RecordedPercentileDay GivenTheDayWasAlreadyRecordedAs(int teamId, DateOnly day, int p50, int p70, int p85, int p95)
        {
            SeedRecordedPercentileDay(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays, day, p50, p70, p85, p95);
            return new RecordedPercentileDay(day, p50, p70, p85, p95);
        }

        /// <summary>
        /// The demo loader backdates invented values into the same table. They are not observations, and
        /// this story must neither trust them nor correct them - it steps over them, as it does over any
        /// day that already carries a row.
        /// </summary>
        private RecordedPercentileDay GivenTheDemoLoaderAlreadyBackdated(int teamId, DateOnly day, int p50, int p70, int p85, int p95)
            => GivenTheDayWasAlreadyRecordedAs(teamId, day, p50, p70, p85, p95);

        private void GivenADatabaseRestoreIsAlreadyRunning()
        {
            var acquired = MaintenanceGate.TryAcquire(DatabaseOperationType.Restore, RestoreOperationId);
            Assert.That(acquired.Acquired, Is.True,
                $"The scenario needs a restore genuinely under way before it starts. It was refused: {acquired.BlockedReason}");
        }

        /// <summary>
        /// Lets the recorder write a day for real, then forgets it - the instance having been switched
        /// off is exactly how these gaps arise. What the recorder produced is returned so the scenario
        /// can hold reconstruction to it.
        /// </summary>
        private async Task<RecordedPercentileDay> GivenADayTheRecorderGenuinelyWroteAndThenLost(int teamId, DateOnly day)
        {
            TheInstanceMovesOnTo(day);
            await TheTeamsRefreshCompletes(teamId);

            var asRecorded = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays)
                .SingleOrDefault(held => held.RecordedAt == day);

            Assert.That(asRecorded, Is.Not.Default,
                $"The recorder wrote no cycle-time day on {day:yyyy-MM-dd}, so there is nothing to hold reconstruction to.");

            TheRecordOfThatPercentileDayIsLost(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays, day);
            TheInstanceMovesOnTo(TodayDay);

            return asRecorded;
        }

        // --- When ---

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheCycleTimeTrend(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.CycleTime, ThirtyDays, from, to);

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        private Task WhenTheTeamsRefreshRuns(int teamId) => TheTeamsRefreshCompletes(teamId);

        // --- Then ---

        private void ThenTheTrendCoversEveryDayFrom(int teamId, DateOnly from, DateOnly to)
        {
            var held = DaysHeldFor(teamId);
            var expected = EveryDayFrom(from, to);

            Assert.That(held, Is.EquivalentTo(expected),
                $"The chart must cover every day between {from:yyyy-MM-dd} and {to:yyyy-MM-dd} that the stored items support. " +
                $"Missing: {string.Join(", ", expected.Except(held))}. Unexpected: {string.Join(", ", held.Except(expected))}.");
        }

        private void ThenTheTrendStopsOn(int teamId, DateOnly lastObservedOn)
        {
            var beyond = DaysHeldFor(teamId).Where(day => day > lastObservedOn).ToList();

            Assert.That(beyond, Is.Empty,
                $"The team was last observed on {lastObservedOn:yyyy-MM-dd}; its items are frozen at that point, so a day past it " +
                $"would be a confident trend over a period nobody watched. Written anyway: {string.Join(", ", beyond)}.");
        }

        private void ThenTheTrendReachesBackNoFurtherThan(int teamId, DateOnly earliestSupportedDay)
        {
            var below = DaysHeldFor(teamId).Where(day => day < earliestSupportedDay).ToList();

            Assert.That(below, Is.Empty,
                $"Nothing was stored before {earliestSupportedDay:yyyy-MM-dd}, so no day before it can be computed from the items. " +
                $"Written anyway: {string.Join(", ", below)}.");
        }

        private void ThenTheChartGainedAtMostOnePassWorthOfDays(int teamId, int heldBefore)
        {
            var added = DaysHeldFor(teamId).Count - heldBefore;

            Assert.That(added, Is.GreaterThan(0).And.LessThanOrEqualTo(ReconstructionCapInDays),
                $"One fill covers at most {ReconstructionCapInDays} days so a year-wide range fills over successive loads " +
                $"rather than in one unbounded walk. Added in one go: {added}.");
        }

        private void ThenOpeningTheTrendAgainKeepsFillingItIn(int teamId, int heldAfterTheFirstFill)
        {
            Assert.That(DaysHeldFor(teamId).Count, Is.GreaterThan(heldAfterTheFirstFill),
                "A range wider than one fill must keep filling on the next load; stopping at the cap for good would leave the " +
                "rest of the range permanently unreachable.");
        }

        private void ThenTheDaysAreLeftAbsent(int teamId, DateOnly from, DateOnly to)
        {
            var present = DaysHeldFor(teamId).Where(day => day >= from && day <= to).ToList();

            Assert.That(present, Is.Empty,
                $"Nothing finished in the thirty days before {from:yyyy-MM-dd}..{to:yyyy-MM-dd}, so every percentile there would " +
                $"read zero. A run of zeros is a more confident falsehood than a gap. Written anyway: {string.Join(", ", present)}.");
        }

        private void ThenNoDayReadsAsFourZeroes(int teamId)
        {
            var zeroes = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays)
                .Where(day => day is { P50: 0, P70: 0, P85: 0, P95: 0 })
                .Select(day => day.RecordedAt)
                .ToList();

            Assert.That(zeroes, Is.Empty,
                $"A day on which nothing finished has no cycle time to report, and four zeros draw a floor the team never had. " +
                $"Written anyway: {string.Join(", ", zeroes)}.");
        }

        private void ThenTheDayStillReadsExactlyAsRecorded(int teamId, RecordedPercentileDay asRecorded)
        {
            var held = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays)
                .Where(day => day.RecordedAt == asRecorded.RecordedAt)
                .ToList();

            Assert.That(held, Is.EqualTo(new[] { asRecorded }),
                $"{asRecorded.RecordedAt:yyyy-MM-dd} was actually observed. Recomputing it against today's configuration and " +
                "today's items is not an improvement on having watched it happen, so the observed row stands.");
        }

        private void ThenTheDayCameBackTheSameAsWhenItWasWatched(int teamId, RecordedPercentileDay asRecorded)
        {
            var held = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays)
                .Where(day => day.RecordedAt == asRecorded.RecordedAt)
                .ToList();

            Assert.That(held, Is.EqualTo(new[] { asRecorded }),
                "A day computed afterwards has to equal the day that was watched, or the chart is quietly mixing two different " +
                "readings of the same thing while claiming they are one.");
        }

        private void ThenTheDayIsHeldExactlyOnce(int teamId, DateOnly day)
        {
            var held = DaysHeldFor(teamId).Count(candidate => candidate == day);

            Assert.That(held, Is.EqualTo(1),
                $"Two fills of {day:yyyy-MM-dd} at the same instant must still leave one point on the chart, not two stacked on " +
                "the same date.");
        }

        private void ThenTheTrendIsUnchangedSince(int teamId, IReadOnlyList<DateOnly> before)
        {
            Assert.That(DaysHeldFor(teamId), Is.EqualTo(before),
                "Everything in the range already had a value, so a second look must cost nothing and change nothing.");
        }

        private void ThenNoDayWasAddedToTheTrend(int teamId, int heldBefore)
        {
            Assert.That(DaysHeldFor(teamId).Count, Is.EqualTo(heldBefore),
                "A fill that keeps writing while the database is being replaced underneath it is the one thing this coupling " +
                "exists to prevent; it must stand down instead, and pick up again on the next load.");
        }

        private static void ThenTheTrendHeldOnlyTheDaysAlreadyWritten(SeriesResponse response, IReadOnlyList<DateOnly> alreadyWritten)
        {
            Assert.That(DatesIn(response), Is.EqualTo(alreadyWritten),
                "The load answers with what the chart holds at that moment. Filling in what is missing happens afterwards, so " +
                "no reader ever waits for it.");
        }

        private void ThenNothingWasWrittenWhileTheReaderWaited(int heldBefore)
        {
            Assert.That(TotalPercentileDaysHeld(), Is.EqualTo(heldBefore),
                "Opening a chart must not write to the database on the thread the reader is waiting on.");
        }

        private void ThenStartingADatabaseRestoreIsRefused()
        {
            var attempt = MaintenanceGate.TryAcquire(DatabaseOperationType.Restore, "restore-attempted-mid-fill");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attempt.Acquired, Is.False,
                    "Replacing the database while the chart is being filled in would swap the file out from under an open " +
                    "transaction. The operator has to be told to wait.");
                Assert.That(attempt.BlockedReason, Is.Not.Null.And.Not.Empty,
                    "A refusal with no reason leaves the operator guessing why the button did nothing.");
            }
        }

        private void ThenStartingADatabaseRestoreIsAllowedAgain()
        {
            var attempt = MaintenanceGate.TryAcquire(DatabaseOperationType.Restore, "restore-attempted-after-fill");

            Assert.That(attempt.Acquired, Is.True,
                $"Once the chart has finished filling in, nothing is holding the database. Refused anyway: {attempt.BlockedReason}");

            MaintenanceGate.Release("restore-attempted-after-fill");
        }

        private void ThenEveryDayIsHeldExactlyOnceAcross(int teamId, DateOnly from, DateOnly to)
        {
            var duplicated = DaysHeldFor(teamId)
                .Where(day => day >= from && day <= to)
                .GroupBy(day => day)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            Assert.That(duplicated, Is.Empty,
                $"A fill and a refresh writing at the same instant must not leave the same day twice. Duplicated: {string.Join(", ", duplicated)}.");
        }

        // --- Shared observation helpers ---

        private List<DateOnly> DaysHeldFor(int teamId)
            => [.. PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, ThirtyDays).Select(day => day.RecordedAt)];

        private static List<DateOnly> EveryDayFrom(DateOnly from, DateOnly to)
        {
            var days = new List<DateOnly>();
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                days.Add(day);
            }

            return days;
        }
    }
}
