using Lighthouse.Backend.API.DTO;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the need over one Refinement cycle. October 2026: Friday 2, Monday 5, Thursday 8,
    /// Monday 12, Thursday 15, Thursday 22. Where a scenario scripts no forecast, and declares no blackout
    /// day, Gravity's one finished Work Item a day makes the shipped engine read one Work Item per working day
    /// at every likelihood. A blackout day changes the history the engine draws from, so those scenarios
    /// script the forecast for the working days they expect.
    /// </summary>
    public partial class NeedOverOneCycleTest
    {
        private const string MondayTheFifth = "2026-10-05";

        private const string ThursdayTheEighth = "2026-10-08";

        private const string MondayTheTwelfth = "2026-10-12";

        private const string ThursdayTheFifteenth = "2026-10-15";

        private const string ThursdayTheTwentySecond = "2026-10-22";

        /// <summary>The piece of set-up a Team lacks, so that the need has no number.</summary>
        public enum MissingSetUp
        {
            Cadence,
            ThroughputHistory,
            RefinementStates,
        }

        // --- Given ---

        /// <summary>Gravity's GR-058 and GR-059 are tagged ready, it refines every Monday and Thursday, and it finished one Work Item a day.</summary>
        private int GivenGravityHasTwoReadyAndRefinesOnMondaysAndThursdays()
        {
            var gravity = Givens.GravityWithItsReadyRule((UserActivityTracking, RefinementPortGivens.ReadyTag), (AdvancedSearch, RefinementPortGivens.ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 1);
            Givens.TheTeamRefines(gravity, RefinementPortGivens.RefinesOnMondaysAndThursdays);
            return gravity;
        }

        /// <summary>Today is Wednesday 7 October 2026, the day the tracker was read.</summary>
        private int GivenATeamWithout(MissingSetUp missing)
        {
            GivenTodayIsOctober(7);

            switch (missing)
            {
                case MissingSetUp.Cadence:
                    var withoutACadence = Givens.GravityWithItsReadyRule((UserActivityTracking, RefinementPortGivens.ReadyTag));
                    TheTeamFinishedWorkEveryDay(withoutACadence, 1);
                    return withoutACadence;
                case MissingSetUp.ThroughputHistory:
                    var withLittleHistory = Givens.GravityWithItsReadyRule((UserActivityTracking, RefinementPortGivens.ReadyTag));
                    TheTeamFinishedWorkEveryDay(withLittleHistory, 3, days: 4);
                    Givens.TheTeamRefines(withLittleHistory, RefinementPortGivens.RefinesOnThursdays);
                    return withLittleHistory;
                default:
                    return Givens.ATeam("Team Zenith", new TeamStates([Backlog], [Implementation], [Done]), ThroughputHistoryDays);
            }
        }

        private void GivenTodayIsOctober(int day) => TodayIs(2026, 10, day);

        private void GivenTheTeamIsLikelyToPull(int overWorkingDays, params (int Percentile, int Count)[] likelihoods)
            => TheTeamIsLikelyToPull(overWorkingDays, likelihoods);

        /// <summary>A system admin has blacked out every Saturday and Sunday, from the week before October 2026 on.</summary>
        private void GivenWeekendsAreBlackedOut()
            => AWeeklyBlackout([DayOfWeek.Saturday, DayOfWeek.Sunday], new DateOnly(2026, 9, 28), last: null, "Weekend");

        private void GivenABlackoutDayOn(DateOnly day) => BlackoutDaysFrom(day, day);

        private void GivenEveryDayIsBlackedOutFrom(DateOnly first, DateOnly last) => BlackoutDaysFrom(first, last);

        private void GivenOnlyThursdayIsBlackedOutByAWeeklyRule(DateOnly thursday)
            => AWeeklyBlackout([DayOfWeek.Thursday], thursday, thursday, "Thursday offsite");

        // --- Then ---

        private static void ThenTheNeedIsBelowFiveToEightOverTheCycle(RefinementViewDto tab, string cycleStart, string cycleEnd)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheNeedOn(tab), Is.EqualTo(new NeedFacts(Below, null, 5, 8, 50, 85, GravitysCycleWorkingDays)),
                    "the range must be the forecast over the seven working days of the cycle, judged against the two ready");
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading(cycleStart, cycleEnd)));
                Assert.That(tab.ReadyCount, Is.EqualTo(2), "the ready count does not change with the cycle");
            }
        }

        private static void ThenTheRangeIsOverTheCycle(RefinementViewDto tab, int low, int high, int workingDays, string cycleStart, string cycleEnd)
        {
            var need = TheNeedOn(tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((need.Low, need.High, need.HorizonWorkingDays), Is.EqualTo(((int?)low, (int?)high, (int?)workingDays)),
                    "the range must be the forecast over exactly the working days of the cycle");
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading(cycleStart, cycleEnd)));
            }
        }

        private static void ThenTheHeadingStillNamesTheNextRefinement(RefinementViewDto tab, string nextRefinement, bool isRefinementDay)
            => Assert.That(new CadenceFactsReading(tab.NextRefinementDate, tab.IsRefinementDay), Is.EqualTo(new CadenceFactsReading(nextRefinement, isRefinementDay)));

        private static void ThenThereIsNoNumberAndNoCycleBecause(RefinementViewDto tab, string reason)
        {
            var need = TheNeedOn(tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.UnavailableReason, Is.EqualTo(reason));
                Assert.That((need.Verdict, need.Low, need.High), Is.EqualTo(((string?)null, (int?)null, (int?)null)));
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading(null, null)), "without a number the need names no cycle");
            }
        }

        private static CycleReading CycleIn(RefinementViewDto tab) => new(tab.Need.CycleStart, tab.Need.CycleEnd);

        /// <summary>The first Refinement of the cycle and the one after it, as days; both null without a range.</summary>
        private sealed record CycleReading(string? Start, string? End);

        private sealed record CadenceFactsReading(string? NextRefinementDate, bool IsRefinementDay);
    }
}
