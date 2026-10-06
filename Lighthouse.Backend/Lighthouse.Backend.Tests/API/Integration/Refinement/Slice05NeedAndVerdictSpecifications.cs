using System.Diagnostics;
using System.Text.Json;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the need number and its verdict. The forecast a verdict is judged against is
    /// scripted per horizon; the parity and the guardrail scenarios run the shipped engine over Work Items
    /// the Team really finished.
    /// </summary>
    public partial class Slice05NeedAndVerdictTest
    {
        private static readonly TimeSpan TwoSeconds = TimeSpan.FromSeconds(2);

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth()
            => await GravityWithTwoReadyRefiningOnThursdaysOnFridayTheSecond();

        private void GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle()
            => TheTeamIsLikelyToPullFiveToEightOverGravitysCycle();

        private async Task<TeamUnderTest> GivenGravityWithoutStagesRefinesOnThursdayTheEighthAndIsLikelyToPullOneToThree()
        {
            var gravity = await GravityRefinesSixWorkItems();
            TheTeamFinishedWorkEveryDay(gravity, 1);
            await TheAdminHasSetTheCadence(gravity, [Thursday], 1, null);
            TodayIs(2026, 10, 2);
            TheTeamIsLikelyToPull(GravitysCycleWorkingDays, (50, 1), (85, 3));
            return gravity;
        }

        /// <summary>Gravity finished two Work Items on each day of its Throughput window and refines on Tuesdays; today is Wednesday 7 October.</summary>
        private async Task<TeamUnderTest> GivenGravityFinishesTwoADayAndRefinesOnTuesdays()
        {
            var gravity = await GravityRefinesSixWorkItems();
            TheTeamFinishedWorkEveryDay(gravity, 2);
            await TheAdminHasSetTheCadence(gravity, [Tuesday], 1, null);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenGravityFinishedWorkOnOnlyFourDaysAndRefinesOnThursdays()
        {
            var gravity = await GravityWithItsReadyRule((UserActivityTracking, ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 3, days: 4);
            await TheAdminHasSetTheCadence(gravity, [Thursday], 1, null);
            return gravity;
        }

        private async Task<TeamUnderTest> GivenGravityHasTwoReadyWithoutACadence()
        {
            var gravity = await GravityWithItsReadyRule((UserActivityTracking, ReadyTag), (AdvancedSearch, ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 1);
            return gravity;
        }

        private TeamUnderTest GivenATeamWithoutRefinementStates()
            => SeedTeam("Team Zenith", [Backlog], [Implementation], [Done], []);

        private async Task<TeamUnderTest> GivenATeamWithThreeHundredWorkItemsInRefinementRefiningOnTuesdays()
        {
            var team = SeedTeam("Team Nebula", [Backlog], [Implementation], [Done], []);
            SeedWorkItems(team, [.. Enumerable.Range(1, 300).Select(number => new TrackerWorkItem(
                $"NB-{number:D3}", $"Nebula Work Item {number}", Backlog, StateCategories.ToDo, $"{number}"))]);
            TheTeamFinishedWorkEveryDay(team, 1);

            await TheAdminHasChosen(team, Backlog);
            await TheAdminHasSetTheCadence(team, [Tuesday], 1, null);
            return team;
        }

        // --- When ---

        private async Task<JsonElement> WhenTheCoachOpensTheRefinementTab(TeamUnderTest team)
            => await ReadTheRefinementTab(team);

        private async Task WhenThreeVotersSayYesOn(TeamUnderTest team, string workItem)
        {
            await HasVoted(Jonas, team, workItem, Answer.Yes);
            await HasVoted(Mo, team, workItem, Answer.Yes);
            await HasVoted(Ana, team, workItem, Answer.Yes);
        }

        private async Task<(JsonElement Tab, TimeSpan Elapsed)> WhenTheTabIsReadASecondTime(TeamUnderTest team)
        {
            await ReadTheRefinementTab(team);

            var stopwatch = Stopwatch.StartNew();
            var tab = await ReadTheRefinementTab(team);
            stopwatch.Stop();

            return (tab, stopwatch.Elapsed);
        }

        // --- Then ---

        private static void ThenTheNeedIs(JsonElement tab, NeedReading expected)
            => Assert.That(NeedIn(tab), Is.EqualTo(expected));

        private static void ThenTheNextRefinementIsThursdayTheEighthWithTwoReady(JsonElement tab)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CadenceFactsIn(tab), Is.EqualTo(new CadenceFactsReading("2026-10-08", false)));
                Assert.That(ReadyCountIn(tab), Is.EqualTo(2));
            }
        }

        private static void ThenTheVerdictIs(JsonElement tab, string verdict)
            => Assert.That(NeedIn(tab).Verdict, Is.EqualTo(verdict));

        private static void ThenTheRangeIs(JsonElement tab, int low, int high, int horizonWorkingDays)
        {
            var need = NeedIn(tab);
            Assert.That((need.Low, need.High, need.HorizonWorkingDays), Is.EqualTo(((int?)low, (int?)high, (int?)horizonWorkingDays)),
                "the range must be the forecast over exactly the working days of the Refinement cycle");
        }

        /// <summary>
        /// The cycle runs from Tuesday 13 to Tuesday 20 October, seven working days; the manual forecast for
        /// seven working days from Wednesday 7 October targets Wednesday 14 October.
        /// </summary>
        private async Task ThenTheRangeIsTheManualForecastForTheSevenWorkingDaysOfTheCycleAfterTuesdayTheThirteenth(JsonElement tab, TeamUnderTest team)
        {
            var need = NeedIn(tab);
            var manual = await TheManualForecastFor(team, new DateOnly(2026, 10, 14));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.Low, Is.EqualTo(HowManyAt(manual, 50)), "the low end is the manual forecast's median");
                Assert.That((need.Low, need.High, need.HorizonWorkingDays), Is.EqualTo(((int?)14, (int?)14, (int?)7)),
                    "two a day over the seven working days of the cycle, read at both ends");
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading("2026-10-13", "2026-10-20")));
            }
        }

        private static void ThenThereIsNoNumberBecause(JsonElement tab, string reason)
        {
            var need = NeedIn(tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.UnavailableReason, Is.EqualTo(reason));
                Assert.That((need.Verdict, need.Low, need.High), Is.EqualTo(((string?)null, (int?)null, (int?)null)),
                    "without a number there is no verdict and no range, so no rows are marked as needed");
            }
        }

        private static void ThenAllSixAreStillListed(JsonElement tab)
            => Assert.That(RowsIn(tab), Has.Count.EqualTo(6));

        private static void ThenTheHighEndIsAndSixAreListed(JsonElement tab, int high)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NeedIn(tab).High, Is.EqualTo(high));
                Assert.That(RowsIn(tab), Has.Count.EqualTo(6));
            }
        }

        private static void ThenTheVerdictCameBackInTime(JsonElement tab, TimeSpan elapsed)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(RowsIn(tab), Has.Count.EqualTo(300));
                Assert.That(NeedIn(tab).Verdict, Is.Not.Null, "the read must have worked out a verdict to have been timed doing so");
                Assert.That(elapsed, Is.LessThan(TwoSeconds));
            }
        }
    }
}
