using System.Text.Json;
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

        private async Task<TeamUnderTest> GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond()
            => await GravityWithTwoReadyRefiningOnThursdaysOnFridayTheSecond();

        /// <summary>Gravity's GR-058 and GR-059 are tagged ready, it refines every Monday and Thursday, and it finished one Work Item a day.</summary>
        private async Task<TeamUnderTest> GivenGravityHasTwoReadyAndRefinesOnMondaysAndThursdays()
        {
            var gravity = await GravityWithItsReadyRule((UserActivityTracking, ReadyTag), (AdvancedSearch, ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 1);
            await TheAdminHasSetTheCadence(gravity, [Monday, Thursday], 1, null);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenATeamWithout(MissingSetUp missing)
        {
            switch (missing)
            {
                case MissingSetUp.Cadence:
                    var withoutACadence = await GravityWithItsReadyRule((UserActivityTracking, ReadyTag));
                    TheTeamFinishedWorkEveryDay(withoutACadence, 1);
                    return withoutACadence;
                case MissingSetUp.ThroughputHistory:
                    var withLittleHistory = await GravityWithItsReadyRule((UserActivityTracking, ReadyTag));
                    TheTeamFinishedWorkEveryDay(withLittleHistory, 3, days: 4);
                    await TheAdminHasSetTheCadence(withLittleHistory, [Thursday], 1, null);
                    return withLittleHistory;
                default:
                    return SeedTeam("Team Zenith", [Backlog], [Implementation], [Done], []);
            }
        }

        private void GivenTodayIsOctober(int day) => TodayIs(2026, 10, day);

        private void GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle() => TheTeamIsLikelyToPullFiveToEightOverGravitysCycle();

        private void GivenTheTeamIsLikelyToPull(int overWorkingDays, params (int Percentile, int Count)[] likelihoods)
            => TheTeamIsLikelyToPull(overWorkingDays, likelihoods);

        private async Task GivenWeekendsAreBlackedOut() => await WeekendsAreBlackedOut();

        private async Task GivenABlackoutDayOn(DateOnly day) => await ABlackoutDayOn(day);

        private async Task GivenEveryDayIsBlackedOutFrom(DateOnly first, DateOnly last) => await BlackoutDaysFrom(first, last);

        // --- When ---

        private async Task<JsonElement> WhenTheCoachOpensTheRefinementTab(TeamUnderTest team)
            => await ReadTheRefinementTab(team);

        // --- Then ---

        private static void ThenTheNeedIsBelowFiveToEightOverTheCycle(JsonElement tab, string cycleStart, string cycleEnd)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NeedIn(tab), Is.EqualTo(new NeedReading(Below, null, 5, 8, 50, 85, GravitysCycleWorkingDays)),
                    "the range must be the forecast over the seven working days of the cycle, judged against the two ready");
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading(cycleStart, cycleEnd)));
                Assert.That(ReadyCountIn(tab), Is.EqualTo(2), "the ready count does not change with the cycle");
            }
        }

        private static void ThenTheRangeIsOverTheCycle(JsonElement tab, int low, int high, int workingDays, string cycleStart, string cycleEnd)
        {
            var need = NeedIn(tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((need.Low, need.High, need.HorizonWorkingDays), Is.EqualTo(((int?)low, (int?)high, (int?)workingDays)),
                    "the range must be the forecast over exactly the working days of the cycle");
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading(cycleStart, cycleEnd)));
            }
        }

        private static void ThenTheHeadingStillNamesTheNextRefinement(JsonElement tab, string nextRefinement, bool isRefinementDay)
            => Assert.That(CadenceFactsIn(tab), Is.EqualTo(new CadenceFactsReading(nextRefinement, isRefinementDay)));

        private static void ThenThereIsNoNumberAndNoCycleBecause(JsonElement tab, string reason)
        {
            var need = NeedIn(tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.UnavailableReason, Is.EqualTo(reason));
                Assert.That((need.Verdict, need.Low, need.High), Is.EqualTo(((string?)null, (int?)null, (int?)null)));
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading(null, null)), "without a number the need names no cycle");
            }
        }
    }
}
