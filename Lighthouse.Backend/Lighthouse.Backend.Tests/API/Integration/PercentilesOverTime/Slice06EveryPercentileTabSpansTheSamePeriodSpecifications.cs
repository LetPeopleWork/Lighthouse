using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for Slice 06 - every tab of the Percentiles Over Time widget covers the same
    /// period, at team scope and at portfolio scope.
    ///
    /// Two things separate this from slice 05. Work item age is not a window of finished items but a
    /// reading of what was in flight on the day itself, so its correctness question is different: was
    /// the item counted as in progress on that day, and at the age it had then. And a portfolio reads
    /// its own deliveries rather than a team's items, through its own service.
    /// </summary>
    public partial class Slice06EveryPercentileTabSpansTheSamePeriodTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        /// <summary>
        /// Work item age has no horizon to look back over - it is read as of the day itself - so it is
        /// filed under the horizon-less sentinel rather than being given an arbitrary one.
        /// </summary>
        private const int NoHorizon = PercentilesOverTimeSnapshot.NoHorizon;

        private static readonly int[] EveryCycleTimeHorizon = [30, 60, 90];

        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        private int GivenAPortfolioStillBeingRefreshed() => SeedPortfolioObservedUntil(TodayDay);

        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-{day:yyyyMMdd}", day.AddDays(-1), day);
            }
        }

        /// <summary>
        /// A stretch where each item took three weeks rather than two days. Without it no look-back can be
        /// told from another: a team that worked at one steady pace reads the same over thirty days as
        /// over ninety, so a reading worked out over the wrong period comes out right anyway.
        /// </summary>
        private void GivenTheTeamTookWeeksOverEachItemFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-slow-{day:yyyyMMdd}", day.AddDays(-20), day);
            }
        }

        private void GivenThePortfolioFinishedOneDeliveryADayFrom(int portfolioId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedDeliveryFinishedOn(portfolioId, $"{portfolioId}-{day:yyyyMMdd}", day.AddDays(-4), day);
            }
        }

        /// <summary>
        /// An item that was in flight for a stretch and then finished. The age it contributes to a day
        /// inside that stretch is its age on that day - not nothing, because it had not finished yet,
        /// and not the age it reached by the end.
        /// </summary>
        private void GivenAnItemThatWasInFlightFrom(int teamId, string referenceId, DateOnly startedOn, DateOnly finishedOn)
            => SeedItemFinishedOn(teamId, referenceId, startedOn, finishedOn);

        private void GivenAnItemStillInFlightSince(int teamId, string referenceId, DateOnly startedOn)
            => SeedItemStillInProgressSince(teamId, referenceId, startedOn);

        private async Task<RecordedPercentileDay> GivenAWorkItemAgeDayTheRecorderWroteAndThenLost(int teamId, DateOnly day)
        {
            TheInstanceMovesOnTo(day);
            await TheTeamsRefreshCompletes(teamId);

            var asRecorded = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.WorkItemAge, NoHorizon)
                .SingleOrDefault(held => held.RecordedAt == day);

            Assert.That(asRecorded, Is.Not.Default,
                $"The recorder wrote no work item age day on {day:yyyy-MM-dd}, so there is nothing to hold reconstruction to.");

            TheRecordOfThatPercentileDayIsLost(teamId, OwnerType.Team, MetricType.WorkItemAge, NoHorizon, day);
            TheInstanceMovesOnTo(TodayDay);

            return asRecorded;
        }

        // --- When ---

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheCycleTimeTab(int teamId, int horizon, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.CycleTime, horizon, from, to);

        private Task<SeriesResponse> WhenTheFlowCoachOpensTheWorkItemAgeTab(int teamId, DateOnly from, DateOnly to)
            => ReadTeamPercentileTrend(teamId, MetricType.WorkItemAge, NoHorizon, from, to);

        private Task<SeriesResponse> WhenTheFlowCoachOpensThePortfolioCycleTimeTab(int portfolioId, int horizon, DateOnly from, DateOnly to)
            => ReadPortfolioPercentileTrend(portfolioId, MetricType.CycleTime, horizon, from, to);

        private Task<SeriesResponse> WhenTheFlowCoachOpensThePortfolioWorkItemAgeTab(int portfolioId, DateOnly from, DateOnly to)
            => ReadPortfolioPercentileTrend(portfolioId, MetricType.WorkItemAge, NoHorizon, from, to);

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        // --- Then ---

        private void ThenTheTabCoversEveryDayFrom(int ownerId, OwnerType ownerType, MetricType metricType, int horizon, DateOnly from, DateOnly to)
        {
            var held = PercentileDaysHeldFor(ownerId, ownerType, metricType, horizon).Select(day => day.RecordedAt).ToList();
            var expected = EveryDayFrom(from, to);

            Assert.That(held, Is.EquivalentTo(expected),
                $"The {metricType} tab must cover every day between {from:yyyy-MM-dd} and {to:yyyy-MM-dd}. " +
                $"Missing: {string.Join(", ", expected.Except(held))}. Unexpected: {string.Join(", ", held.Except(expected))}.");
        }

        private void ThenEveryTabCoversTheSamePeriod(int teamId, DateOnly from, DateOnly to)
        {
            var spans = EveryCycleTimeHorizon
                .Select(horizon => (Tab: $"cycle time over {horizon} days", Days: DaysOn(teamId, MetricType.CycleTime, horizon)))
                .Append((Tab: "work item age", Days: DaysOn(teamId, MetricType.WorkItemAge, NoHorizon)))
                .ToList();

            var expected = EveryDayFrom(from, to);

            using (Assert.EnterMultipleScope())
            {
                foreach (var span in spans)
                {
                    Assert.That(span.Days, Is.EquivalentTo(expected),
                        $"Comparing age against finished cycle time only works when both tabs cover the same period. " +
                        $"The {span.Tab} tab does not. Missing: {string.Join(", ", expected.Except(span.Days))}.");
                }
            }
        }

        private void ThenTheLongerLookBackReadsHigherOn(int teamId, DateOnly day, int shorter, int longer)
        {
            var overTheShorterOne = TheCycleTimeReadOn(teamId, day, shorter);
            var overTheLongerOne = TheCycleTimeReadOn(teamId, day, longer);

            Assert.That(overTheLongerOne.P85, Is.GreaterThan(overTheShorterOne.P85),
                $"On {day:yyyy-MM-dd} the team's last {longer} days still contain the slow stretch and its last " +
                $"{shorter} do not, so the two look-backs cannot report the same figure. Both came out at " +
                $"{overTheShorterOne.P85}, which means they summarised one and the same stretch of work - whatever " +
                "period each row is filed under, only one period was ever measured.");
        }

        private RecordedPercentileDay TheCycleTimeReadOn(int teamId, DateOnly day, int horizon)
        {
            var held = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.CycleTime, horizon)
                .SingleOrDefault(candidate => candidate.RecordedAt == day);

            Assert.That(held, Is.Not.Default,
                $"The cycle time tab over {horizon} days holds nothing for {day:yyyy-MM-dd}, so there is no reading to compare.");

            return held;
        }

        private void ThenTheItemCountsTowardsThatDayAtTheAgeItHadThen(int teamId, DateOnly day, int expectedAge)
        {
            var held = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.WorkItemAge, NoHorizon)
                .SingleOrDefault(candidate => candidate.RecordedAt == day);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(held, Is.Not.Default,
                    $"An item was in flight on {day:yyyy-MM-dd}, so that day has an age to report and must not be blank.");
                Assert.That(held.P50, Is.EqualTo(expectedAge),
                    $"The age reported for {day:yyyy-MM-dd} must be the age the item had on that day - neither nothing, " +
                    "because it had not finished yet, nor the age it went on to reach.");
            }
        }

        private void ThenTheDayCameBackTheSameAsWhenItWasWatched(int teamId, RecordedPercentileDay asWatched)
        {
            var held = PercentileDaysHeldFor(teamId, OwnerType.Team, MetricType.WorkItemAge, NoHorizon)
                .Where(day => day.RecordedAt == asWatched.RecordedAt)
                .ToList();

            Assert.That(held, Is.EqualTo(new[] { asWatched }),
                "Age read as of a past day must equal the age that was watched on that day, or the reading is silently " +
                "anchored to the present instead.");
        }

        private void ThenTheTabIsUnchangedSince(int teamId, MetricType metricType, int horizon, IReadOnlyList<DateOnly> before)
        {
            Assert.That(DaysOn(teamId, metricType, horizon), Is.EqualTo(before),
                "Switching between tabs asks about the same owner over the same period, so the second tab must find the work " +
                "already done rather than start it again.");
        }

        private void ThenTheTabStopsOn(int ownerId, OwnerType ownerType, MetricType metricType, int horizon, DateOnly lastObservedOn)
        {
            var beyond = PercentileDaysHeldFor(ownerId, ownerType, metricType, horizon)
                .Select(day => day.RecordedAt)
                .Where(day => day > lastObservedOn)
                .ToList();

            Assert.That(beyond, Is.Empty,
                $"Nothing has been observed since {lastObservedOn:yyyy-MM-dd}, so no day past it can be reported. " +
                $"Written anyway: {string.Join(", ", beyond)}.");
        }

        private void ThenNoDayOnThatTabReadsAsFourZeroes(int ownerId, OwnerType ownerType, MetricType metricType, int horizon)
        {
            var zeroes = PercentileDaysHeldFor(ownerId, ownerType, metricType, horizon)
                .Where(day => day is { P50: 0, P70: 0, P85: 0, P95: 0 })
                .Select(day => day.RecordedAt)
                .ToList();

            Assert.That(zeroes, Is.Empty,
                $"A day with nothing to measure has no percentile to report, and four zeros draw a floor nobody had. " +
                $"Written anyway on the {metricType} tab: {string.Join(", ", zeroes)}.");
        }

        // --- Shared observation helpers ---

        private List<DateOnly> DaysOn(int teamId, MetricType metricType, int horizon)
            => [.. PercentileDaysHeldFor(teamId, OwnerType.Team, metricType, horizon).Select(day => day.RecordedAt)];

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
