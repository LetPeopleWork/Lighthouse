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
        /// <summary>
        /// Gravity refines every Thursday. From any day up to Thursday 8 October its cycle runs from that
        /// Thursday to Thursday 15 October: the seven days after the 8th, none of them blacked out.
        /// </summary>
        private const int GravitysCycleWorkingDays = 7;

        private const string ReadyTag = "ready";

        private const string Below = "Below";

        private const string NoCadence = "NoCadence";

        private const string InsufficientData = "InsufficientData";

        private const string NoRefinementStates = "NoRefinementStates";

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

        /// <summary>
        /// Gravity's GR-058 and GR-059 are tagged ready under a Ready rule, it refines on Thursdays every week,
        /// and today is Friday 2 October 2026 - six days before Thursday 8 October. It finished one Work Item a
        /// day over its Throughput window, which is enough history for a forecast.
        /// </summary>
        private int GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond()
        {
            var gravity = GravityWithItsReadyRule((UserActivityTracking, ReadyTag), (AdvancedSearch, ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 1);
            Givens.TheTeamRefines(gravity, RefinementPortGivens.RefinesOnThursdays);
            GivenTodayIsOctober(2);
            return gravity;
        }

        /// <summary>Gravity's GR-058 and GR-059 are tagged ready, it refines every Monday and Thursday, and it finished one Work Item a day.</summary>
        private int GivenGravityHasTwoReadyAndRefinesOnMondaysAndThursdays()
        {
            var gravity = GravityWithItsReadyRule((UserActivityTracking, ReadyTag), (AdvancedSearch, ReadyTag));
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
                    var withoutACadence = GravityWithItsReadyRule((UserActivityTracking, ReadyTag));
                    TheTeamFinishedWorkEveryDay(withoutACadence, 1);
                    return withoutACadence;
                case MissingSetUp.ThroughputHistory:
                    var withLittleHistory = GravityWithItsReadyRule((UserActivityTracking, ReadyTag));
                    TheTeamFinishedWorkEveryDay(withLittleHistory, 3, days: 4);
                    Givens.TheTeamRefines(withLittleHistory, RefinementPortGivens.RefinesOnThursdays);
                    return withLittleHistory;
                default:
                    return Givens.ATeam("Team Zenith", new TeamStates([Backlog], [Implementation], [Done]), ThroughputHistoryDays);
            }
        }

        private int GravityWithItsReadyRule(params (string WorkItem, string Tag)[] tags)
        {
            var gravity = GravityRefinesSixWorkItems(tags);
            Givens.TheTeamRefines(gravity, RefinementPortGivens.ReadyWhenTaggedReady);
            return gravity;
        }

        private void GivenTodayIsOctober(int day) => TodayIs(2026, 10, day);

        /// <summary>
        /// Over the working days of Gravity's cycle the Team is likely to pull a different number at every
        /// likelihood: 3 at 30%, 5 at the median, 6 at 70%, 8 at 85% and 10 at 95%.
        /// </summary>
        private void GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle()
            => TheTeamIsLikelyToPull(GravitysCycleWorkingDays, (30, 3), (50, 5), (70, 6), (85, 8), (95, 10));

        private void GivenTheTeamIsLikelyToPull(int overWorkingDays, params (int Percentile, int Count)[] likelihoods)
            => TheTeamIsLikelyToPull(overWorkingDays, likelihoods);

        /// <summary>A system admin has blacked out every Saturday and Sunday, from the week before October 2026 on.</summary>
        private void GivenWeekendsAreBlackedOut()
            => AWeeklyBlackout([DayOfWeek.Saturday, DayOfWeek.Sunday], new DateOnly(2026, 9, 28), last: null, "Weekend");

        private void GivenABlackoutDayOn(DateOnly day) => BlackoutDaysFrom(day, day);

        private void GivenEveryDayIsBlackedOutFrom(DateOnly first, DateOnly last) => BlackoutDaysFrom(first, last);

        private void GivenOnlyThursdayIsBlackedOutByAWeeklyRule(DateOnly thursday)
            => AWeeklyBlackout([DayOfWeek.Thursday], thursday, thursday, "Thursday offsite");

        // --- When ---

        /// <summary>The tab as the browser receives it, before it is written out as JSON.</summary>
        private RefinementViewDto WhenTheCoachOpensTheRefinementTab(int team)
        {
            var view = TheRefinementTabOf(team) ?? throw new InvalidOperationException($"no Team {team} is stored, so it has no Refinement tab");
            return new RefinementViewDto(view);
        }

        // --- Then ---

        private static void ThenTheNeedIsBelowFiveToEightOverTheCycle(RefinementViewDto tab, string cycleStart, string cycleEnd)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NeedIn(tab), Is.EqualTo(new NeedReading(Below, null, 5, 8, 50, 85, GravitysCycleWorkingDays)),
                    "the range must be the forecast over the seven working days of the cycle, judged against the two ready");
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading(cycleStart, cycleEnd)));
                Assert.That(tab.ReadyCount, Is.EqualTo(2), "the ready count does not change with the cycle");
            }
        }

        private static void ThenTheRangeIsOverTheCycle(RefinementViewDto tab, int low, int high, int workingDays, string cycleStart, string cycleEnd)
        {
            var need = NeedIn(tab);

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
            var need = NeedIn(tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.UnavailableReason, Is.EqualTo(reason));
                Assert.That((need.Verdict, need.Low, need.High), Is.EqualTo(((string?)null, (int?)null, (int?)null)));
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading(null, null)), "without a number the need names no cycle");
            }
        }

        private static NeedReading NeedIn(RefinementViewDto tab)
            => new(
                tab.Need.Verdict?.ToString(),
                tab.Need.UnavailableReason?.ToString(),
                tab.Need.Low,
                tab.Need.High,
                tab.Need.LowPercentile,
                tab.Need.HighPercentile,
                tab.Need.HorizonWorkingDays);

        private static CycleReading CycleIn(RefinementViewDto tab) => new(tab.Need.CycleStart, tab.Need.CycleEnd);

        /// <summary>The verdict or why there is none, the range and its likelihoods, and the working days it covers.</summary>
        private sealed record NeedReading(
            string? Verdict,
            string? UnavailableReason,
            int? Low,
            int? High,
            int? LowPercentile,
            int? HighPercentile,
            int? HorizonWorkingDays);

        /// <summary>The first Refinement of the cycle and the one after it, as days; both null without a range.</summary>
        private sealed record CycleReading(string? Start, string? End);

        private sealed record CadenceFactsReading(string? NextRefinementDate, bool IsRefinementDay);
    }
}
