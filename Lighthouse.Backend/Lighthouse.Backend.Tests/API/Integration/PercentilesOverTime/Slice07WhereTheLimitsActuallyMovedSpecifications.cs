using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// Step definitions for Slice 07 - the natural process limits a delivery lead needs in order to tell
    /// a genuine shift from one odd week.
    ///
    /// The family sets are stated here as the behaviour they stand for rather than as a reference to
    /// where the product happens to keep them: a team reports five behaviours, a portfolio six, and the
    /// sixth is about the size of deliveries, which a team does not have. Naming the sets here means
    /// dropping one is a failing scenario rather than a capability that quietly goes missing.
    /// </summary>
    public partial class Slice07WhereTheLimitsActuallyMovedTest : ReconstructOverTimeHistoryAcceptanceTest
    {
        private static readonly ProcessBehaviorMetricType[] EveryBehaviourATeamReports =
        [
            ProcessBehaviorMetricType.Throughput,
            ProcessBehaviorMetricType.WorkItemAge,
            ProcessBehaviorMetricType.Wip,
            ProcessBehaviorMetricType.CycleTime,
            ProcessBehaviorMetricType.Arrivals,
        ];

        private static readonly ProcessBehaviorMetricType[] EveryBehaviourAPortfolioReports =
        [
            ProcessBehaviorMetricType.Throughput,
            ProcessBehaviorMetricType.WorkItemAge,
            ProcessBehaviorMetricType.Wip,
            ProcessBehaviorMetricType.CycleTime,
            ProcessBehaviorMetricType.Arrivals,
            ProcessBehaviorMetricType.FeatureSize,
        ];

        // --- Given ---

        private int GivenATeamStillBeingRefreshed() => SeedTeamObservedUntil(TodayDay);

        private int GivenAPortfolioStillBeingRefreshed() => SeedPortfolioObservedUntil(TodayDay);

        private int GivenATeamLastObservedOn(DateOnly lastObservedOn) => SeedTeamObservedUntil(lastObservedOn);

        private void GivenTheTeamFinishedOneItemADayFrom(int teamId, DateOnly from, DateOnly to)
        {
            for (var day = from; day <= to; day = day.AddDays(1))
            {
                SeedItemFinishedOn(teamId, $"{teamId}-{day:yyyyMMdd}", day.AddDays(-1), day);
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
        /// The lead has fixed the stretch the limits are drawn from, rather than letting it follow the
        /// window. A fixed reference period does not move, so the limits drawn from it should not move
        /// either - and that is a reading, not an absence.
        /// </summary>
        private void GivenTheTeamPinnedTheStretchItsLimitsAreDrawnFrom(int teamId, DateOnly from, DateOnly to)
            => PinTheTeamsBaselineTo(teamId, from, to);

        private void GivenThePortfolioPinnedTheStretchItsLimitsAreDrawnFrom(int portfolioId, DateOnly from, DateOnly to)
            => PinThePortfoliosBaselineTo(portfolioId, from, to);

        /// <summary>
        /// A reference stretch that reaches back further than the owner keeps finished work. Nothing can
        /// be drawn from a period the owner no longer holds, so those days have no limits to report.
        /// </summary>
        private void GivenTheTeamPinnedAStretchItNoLongerKeepsAnyWorkFrom(int teamId)
            => PinTheTeamsBaselineTo(teamId, TodayDay.AddDays(-900), TodayDay.AddDays(-800));

        private async Task<RecordedLimitDay> GivenALimitDayTheRecorderWroteAndThenLost(int teamId, ProcessBehaviorMetricType behaviour, DateOnly day)
        {
            TheInstanceMovesOnTo(day);
            await TheTeamsRefreshCompletes(teamId);

            var asRecorded = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .SingleOrDefault(held => held.RecordedAt == day);

            Assert.That(asRecorded, Is.Not.Default,
                $"The recorder wrote no {behaviour} limits on {day:yyyy-MM-dd}, so there is nothing to hold reconstruction to.");

            TheRecordOfThatLimitDayIsLost(teamId, OwnerType.Team, behaviour, day);
            TheInstanceMovesOnTo(TodayDay);

            return asRecorded;
        }

        // --- When ---

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensTheTeamLimits(int teamId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
            => ReadTeamLimitTrend(teamId, behaviour, from, to);

        private Task<SeriesResponse> WhenTheDeliveryLeadOpensThePortfolioLimits(int portfolioId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
            => ReadPortfolioLimitTrend(portfolioId, behaviour, from, to);

        private async Task WhenTheDeliveryLeadTogglesEveryTeamBehaviour(int teamId, DateOnly from, DateOnly to)
        {
            foreach (var behaviour in EveryBehaviourATeamReports)
            {
                await ReadTeamLimitTrend(teamId, behaviour, from, to);
            }
        }

        private async Task WhenTheDeliveryLeadTogglesEveryPortfolioBehaviour(int portfolioId, DateOnly from, DateOnly to)
        {
            foreach (var behaviour in EveryBehaviourAPortfolioReports)
            {
                await ReadPortfolioLimitTrend(portfolioId, behaviour, from, to);
            }
        }

        private Task WhenTheChartHasFinishedFillingIn() => TheReconstructionPassRunsToCompletion();

        // --- Then ---

        private void ThenEveryBehaviourTheTeamReportsGotItsLimits(int teamId)
        {
            Assert.That(LimitFamiliesHeldFor(teamId, OwnerType.Team), Is.EquivalentTo(EveryBehaviourATeamReports),
                "A team reports five behaviours when it records today, so it must fill in all five for past days too. " +
                "Filling in four of them is a capability that goes missing without anyone noticing.");
        }

        private void ThenEveryBehaviourThePortfolioReportsGotItsLimits(int portfolioId)
        {
            Assert.That(LimitFamiliesHeldFor(portfolioId, OwnerType.Portfolio), Is.EquivalentTo(EveryBehaviourAPortfolioReports),
                "A portfolio reports six behaviours when it records today, so it must fill in all six for past days too.");
        }

        private void ThenDeliverySizeIsReportedForThePortfolioAndNotForTheTeam(int teamId, int portfolioId)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(LimitDaysHeldFor(portfolioId, OwnerType.Portfolio, ProcessBehaviorMetricType.FeatureSize), Is.Not.Empty,
                    "How big deliveries are getting is a portfolio question, and the portfolio must answer it for past days.");
                Assert.That(LimitDaysHeldFor(teamId, OwnerType.Team, ProcessBehaviorMetricType.FeatureSize), Is.Empty,
                    "A team has no deliveries to size, so filling in a size history for one would invent a behaviour it does not have.");
            }
        }

        private void ThenNoLimitsAreReportedFor(int teamId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
        {
            var present = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Select(day => day.RecordedAt)
                .Where(day => day >= from && day <= to)
                .ToList();

            Assert.That(present, Is.Empty,
                $"There is nothing to draw {behaviour} limits from over {from:yyyy-MM-dd}..{to:yyyy-MM-dd}, and three flat lines " +
                $"pinned at zero would describe a process nobody had. Written anyway: {string.Join(", ", present)}.");
        }

        private void ThenNoDayReportsACollapsedBand(int teamId, ProcessBehaviorMetricType behaviour)
        {
            // The lower limit is deliberately not part of this: a busy process routinely reports a lower
            // limit of zero, because the calculation will not go below it for counts that cannot go
            // negative. It is the average and the upper limit both being zero that means "no process".
            var collapsed = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Where(day => day is { Average: 0, Unpl: 0 })
                .Select(day => day.RecordedAt)
                .ToList();

            Assert.That(collapsed, Is.Empty,
                $"A band with no width and no centre is not a process anyone had. Written anyway: {string.Join(", ", collapsed)}.");
        }

        private void ThenTheLimitsHoldSteadyAcross(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
        {
            var reported = LimitDaysHeldFor(ownerId, ownerType, behaviour)
                .Where(day => day.RecordedAt >= from && day.RecordedAt <= to)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reported, Is.Not.Empty,
                    "A fixed reference stretch still describes a process. Reporting nothing at all reads as \"your data did not " +
                    "support it\", which is a different and false statement.");
                Assert.That(reported.Select(day => (day.Unpl, day.Average, day.Lnpl)).Distinct().Count(), Is.EqualTo(1),
                    "The stretch the limits are drawn from was fixed, so the limits drawn from it cannot move day to day.");
            }
        }

        private void ThenTheLimitsAreFreeToMoveAcross(int teamId, ProcessBehaviorMetricType behaviour, DateOnly from, DateOnly to)
        {
            var reported = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Where(day => day.RecordedAt >= from && day.RecordedAt <= to)
                .ToList();

            Assert.That(reported, Is.Not.Empty,
                "Without a fixed reference stretch each day draws its limits from its own recent history, so the days must be " +
                "there to be compared at all.");
        }

        private void ThenTheLimitsCameBackTheSameAsWhenTheyWereWatched(int teamId, ProcessBehaviorMetricType behaviour, RecordedLimitDay asWatched)
        {
            var held = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Where(day => day.RecordedAt == asWatched.RecordedAt)
                .ToList();

            Assert.That(held, Is.EqualTo(new[] { asWatched }),
                "Limits worked out afterwards must equal the limits that were watched, or the three lines mix two different " +
                "readings while claiming to be one history.");
        }

        private void ThenTheLimitsStopOn(int teamId, ProcessBehaviorMetricType behaviour, DateOnly lastObservedOn)
        {
            var beyond = LimitDaysHeldFor(teamId, OwnerType.Team, behaviour)
                .Select(day => day.RecordedAt)
                .Where(day => day > lastObservedOn)
                .ToList();

            Assert.That(beyond, Is.Empty,
                $"Nothing has been observed since {lastObservedOn:yyyy-MM-dd}, so limits past it would describe a period nobody " +
                $"watched. Written anyway: {string.Join(", ", beyond)}.");
        }

        private void ThenTheLimitsAreUnchangedSince(int teamId, ProcessBehaviorMetricType behaviour, IReadOnlyList<RecordedLimitDay> before)
        {
            Assert.That(LimitDaysHeldFor(teamId, OwnerType.Team, behaviour), Is.EqualTo(before),
                "Everything in the period already had limits, so a second look must cost nothing and change nothing.");
        }

        private IReadOnlyList<RecordedLimitDay> LimitsReportedFor(int teamId, ProcessBehaviorMetricType behaviour)
            => LimitDaysHeldFor(teamId, OwnerType.Team, behaviour);
    }
}
