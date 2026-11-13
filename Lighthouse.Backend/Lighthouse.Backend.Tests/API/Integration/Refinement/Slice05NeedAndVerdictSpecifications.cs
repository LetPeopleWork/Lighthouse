using System.Diagnostics;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Refinement;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the need number and its verdict. The forecast a verdict is judged against is
    /// scripted per horizon; the guardrail scenario runs the shipped engine over Work Items the Team really
    /// finished. Every Given says which day it is: Wednesday 7 October 2026 unless it names another. The tab
    /// is read as the browser receives it, before it is written out as JSON.
    /// </summary>
    public partial class Slice05NeedAndVerdictTest
    {
        private static readonly TimeSpan TwoSeconds = TimeSpan.FromSeconds(2);

        private static readonly PortVoter Jonas = ABrowserOf("Jonas Weber");

        private static readonly PortVoter Ana = ABrowserOf("Ana Lima");

        private static readonly PortVoter Mo = ABrowserOf("Mo Okafor");

        // --- Given ---

        /// <summary>Over the working days of Gravity's cycle the Team is likely to pull the low end at the median and the high end at 85%.</summary>
        private void GivenTheTeamIsLikelyToPullOverItsCycle(int low, int high)
            => TheTeamIsLikelyToPull(GravitysCycleWorkingDays, (50, low), (85, high));

        /// <summary>Gravity sets no stage rule, so votes decide what is ready; it refines on Thursdays and today is Friday 2 October.</summary>
        private int GivenGravityWithoutStagesRefinesOnThursdayTheEighthAndIsLikelyToPullOneToThree()
        {
            var gravity = GravityRefinesSixWorkItems();
            TheTeamFinishedWorkEveryDay(gravity, 1);
            Givens.TheTeamRefines(gravity, RefinementPortGivens.RefinesOnThursdays);
            TodayIs(2026, 10, 2);
            TheTeamIsLikelyToPull(GravitysCycleWorkingDays, (50, 1), (85, 3));
            return gravity;
        }

        private int GivenGravityFinishedWorkOnOnlyFourDaysAndRefinesOnThursdays()
        {
            GivenTodayIsWednesdayTheSeventhOfOctober();
            var gravity = Givens.GravityWithItsReadyRule((UserActivityTracking, RefinementPortGivens.ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 3, days: 4);
            Givens.TheTeamRefines(gravity, RefinementPortGivens.RefinesOnThursdays);
            return gravity;
        }

        private int GivenGravityHasTwoReadyWithoutACadence()
        {
            GivenTodayIsWednesdayTheSeventhOfOctober();
            var gravity = Givens.GravityWithItsReadyRule((UserActivityTracking, RefinementPortGivens.ReadyTag), (AdvancedSearch, RefinementPortGivens.ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 1);
            return gravity;
        }

        private int GivenATeamWithoutRefinementStates()
        {
            GivenTodayIsWednesdayTheSeventhOfOctober();
            return Givens.ATeam("Team Zenith", new TeamStates([Backlog], [Implementation], [Done]), ThroughputHistoryDays);
        }

        /// <summary>Team Nebula's backlog holds three hundred Work Items, it refines in Backlog every Tuesday, and it finished one Work Item a day.</summary>
        private int GivenATeamWithThreeHundredWorkItemsInRefinementRefiningOnTuesdays()
        {
            GivenTodayIsWednesdayTheSeventhOfOctober();
            var nebula = Givens.ATeamWhoseBacklogHolds("Team Nebula", workItems: 300);
            TheTeamFinishedWorkEveryDay(nebula, 1);
            Givens.TheTeamRefines(nebula, RefinementPortGivens.RefinesInBacklog, RefinementPortGivens.RefinesOnTuesdays);
            return nebula;
        }

        /// <summary>Today is Wednesday 7 October 2026, nine in the morning, unless a Given names another day.</summary>
        private void GivenTodayIsWednesdayTheSeventhOfOctober() => TodayIs(2026, 10, 7);

        // --- When ---

        private void WhenThreeVotersSayYesOn(int team, string workItem)
        {
            HasVoted(Jonas, team, workItem, SizingAnswer.Yes);
            HasVoted(Mo, team, workItem, SizingAnswer.Yes);
            HasVoted(Ana, team, workItem, SizingAnswer.Yes);
        }

        private (RefinementViewDto Tab, TimeSpan Elapsed) WhenTheTabIsReadASecondTime(int team)
        {
            WhenTheCoachOpensTheRefinementTab(team);

            var stopwatch = Stopwatch.StartNew();
            var tab = WhenTheCoachOpensTheRefinementTab(team);
            stopwatch.Stop();

            return (tab, stopwatch.Elapsed);
        }

        // --- Then ---

        private static void ThenTheNeedIs(RefinementViewDto tab, NeedFacts expected)
            => Assert.That(TheNeedOn(tab), Is.EqualTo(expected),
                "the need must judge the ready Work Items against the range read at the median and at 85% over the working days of the cycle");

        private static void ThenTheNextRefinementIsThursdayTheEighthWithTwoReady(RefinementViewDto tab)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((tab.NextRefinementDate, tab.IsRefinementDay), Is.EqualTo(((string?)"2026-10-08", false)),
                    "from Friday the 2nd the next Refinement of a Team that refines on Thursdays is Thursday the 8th");
                Assert.That(tab.ReadyCount, Is.EqualTo(2), "GR-058 and GR-059 are tagged ready under the Ready rule");
            }
        }

        private static void ThenTheVerdictIs(RefinementViewDto tab, string verdict)
            => Assert.That(TheNeedOn(tab).Verdict, Is.EqualTo(verdict),
                "the ready count is below, in or above the range, both ends included in range");

        private static void ThenTheRangeIs(RefinementViewDto tab, int low, int high, int horizonWorkingDays)
        {
            var need = TheNeedOn(tab);
            Assert.That((need.Low, need.High, need.HorizonWorkingDays), Is.EqualTo(((int?)low, (int?)high, (int?)horizonWorkingDays)),
                "the range must be the forecast over exactly the working days of the Refinement cycle");
        }

        private static void ThenThereIsNoNumberBecause(RefinementViewDto tab, string reason)
        {
            var need = TheNeedOn(tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.UnavailableReason, Is.EqualTo(reason), "the tab must say why it gives no number");
                Assert.That((need.Verdict, need.Low, need.High), Is.EqualTo(((string?)null, (int?)null, (int?)null)),
                    "without a number there is no verdict and no range, so no rows are marked as needed");
            }
        }

        private static void ThenAllSixAreStillListed(RefinementViewDto tab)
            => Assert.That(tab.WorkItems, Has.Count.EqualTo(6), "a missing number never hides the Work Items in refinement");

        private static void ThenTheHighEndIsAndSixAreListed(RefinementViewDto tab, int high)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheNeedOn(tab).High, Is.EqualTo(high), "the high end is stated as forecast, never cut down to the Work Items listed");
                Assert.That(tab.WorkItems, Has.Count.EqualTo(6));
            }
        }

        private static void ThenTheVerdictCameBackInTime(RefinementViewDto tab, TimeSpan elapsed)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(tab.WorkItems, Has.Count.EqualTo(300));
                Assert.That(TheNeedOn(tab).Verdict, Is.Not.Null, "the read must have worked out a verdict to have been timed doing so");
                Assert.That(elapsed, Is.LessThan(TwoSeconds), "a warm read of three hundred Work Items in refinement must answer within two seconds");
            }
        }
    }
}
