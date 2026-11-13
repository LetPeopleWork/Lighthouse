using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Before the next Refinement the tab says whether to refine more or stop. The number is the Team's own
    /// manual How Many forecast for the working days of one Refinement cycle, from the next Refinement up to
    /// and including the one after it (from today, on a Refinement day): the low end is what the Team
    /// pulls more likely than not (the median), the high end a count only 15% of runs pull more than.
    /// The ready Work Items - by votes on a Team without stage rules, by stage on a Team with them - are
    /// below, in or above that range, ends included in range. The answer is facts only: counts, the range,
    /// its percentiles, the date, the working days and a verdict; the browser and the clients say it in
    /// words. Where no number can be given the tab says why: no cadence, too little Throughput history (the
    /// same guard forecasts use), or no refinement states. The high end is stated as forecast, never cut down
    /// to the Work Items listed: the browser numbers that many rows in whatever order it shows them and draws
    /// the "enough for" line after them, or after the last row when fewer are listed.
    ///
    /// Driven at the application-service port: a vote is cast through the sizing log's commands for a voter
    /// established as the votes route establishes one, and the tab is read through its query as the browser
    /// receives it. That the tab's range is what the manual forecast route answers, and the texts of the
    /// reasons for no number, are pinned on the wire contract of the Refinement tab. Step definitions live in
    /// Slice05NeedAndVerdictSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-05")]
    public partial class Slice05NeedAndVerdictTest : RefinementAtThePortTest
    {
        // --- The verdict ---

        // Two Work Items are ready; both ends of the range belong to it.
        [TestCase(3, 8, Below)]
        [TestCase(2, 8, InRange)]
        [TestCase(1, 2, InRange)]
        [TestCase(0, 1, Above)]
        public void The_verdict_compares_the_ready_count_with_both_ends_of_the_range(int low, int high, string verdict)
        {
            var gravity = GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            GivenTheTeamIsLikelyToPullOverItsCycle(low, high);

            var tab = WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheVerdictIs(tab, verdict);
        }

        // Without stage rules the votes make Work Items Ready, and so they move the verdict.
        [Test]
        public void On_a_Team_without_stages_a_vote_that_makes_a_Work_Item_Ready_moves_the_verdict()
        {
            var gravity = GivenGravityWithoutStagesRefinesOnThursdayTheEighthAndIsLikelyToPullOneToThree();
            ThenTheVerdictIs(WhenTheCoachOpensTheRefinementTab(gravity), Below);

            WhenThreeVotersSayYesOn(gravity, RefinementPortGivens.ConfigurationManagement);

            ThenTheVerdictIs(WhenTheCoachOpensTheRefinementTab(gravity), InRange);
        }

        // With stage rules the tracker decides what is ready; votes are shown, not counted. Were they counted,
        // the three Yes votes would make a third Work Item ready and lift the count above the range of 1-2.
        [Test]
        public void On_a_Team_with_stages_votes_do_not_move_the_verdict()
        {
            var gravity = GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            GivenTheTeamIsLikelyToPullOverItsCycle(1, 2);

            WhenThreeVotersSayYesOn(gravity, RefinementPortGivens.ConfigurationManagement);

            ThenTheVerdictIs(WhenTheCoachOpensTheRefinementTab(gravity), InRange);
        }

        // --- The number behind it ---

        // Tuesday 13 and Wednesday 14 October are blackout days, so only five of the seven days after
        // Thursday the 8th up to Thursday the 15th are working days.
        [Test]
        public void Blackout_days_inside_the_cycle_are_not_counted()
        {
            var gravity = GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            BlackoutDaysFrom(new DateOnly(2026, 10, 13), new DateOnly(2026, 10, 14));
            TheTeamIsLikelyToPull(5, (50, 4), (85, 7));

            var tab = WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIs(tab, low: 4, high: 7, horizonWorkingDays: 5);
        }

        // On a Refinement day today's session is the moment to top up, so the count looks a week ahead.
        [Test]
        public void On_a_Refinement_day_the_number_is_for_the_following_Refinement()
        {
            var gravity = GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            TodayIs(2026, 10, 8);
            TheTeamIsLikelyToPull(7, (50, 6), (85, 9));

            var tab = WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIs(tab, low: 6, high: 9, horizonWorkingDays: 7);
        }

        // --- When there is no number ---

        [Test]
        public void Without_a_cadence_there_is_no_number_and_the_list_stays()
        {
            var gravity = GivenGravityHasTwoReadyWithoutACadence();

            var tab = WhenTheCoachOpensTheRefinementTab(gravity);

            ThenThereIsNoNumberBecause(tab, NoCadence);
            ThenAllSixAreStillListed(tab);
        }

        // --- How many rows are needed ---

        // Gravity lists six Work Items. The browser needs the true high end to say that all six are needed,
        // so the answer never cuts it down to what is listed.
        [TestCase(3)]
        [TestCase(6)]
        [TestCase(11)]
        public void The_high_end_is_stated_as_forecast_whatever_number_of_Work_Items_is_listed(int high)
        {
            var gravity = GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            GivenTheTeamIsLikelyToPullOverItsCycle(1, high);

            var tab = WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheHighEndIsAndSixAreListed(tab, high);
        }

        // A Team likely to pull nothing at all over the cycle needs nothing refined.
        [Test]
        public void A_range_of_nothing_says_stop_with_two_ready()
        {
            var gravity = GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            GivenTheTeamIsLikelyToPullOverItsCycle(0, 0);

            var tab = WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNeedIs(tab, new NeedFacts(Above, null, 0, 0, 50, 85, GravitysCycleWorkingDays));
        }

        // --- Guardrail ---

        // Second, warm read; the shipped forecast engine runs.
        [Test]
        public void Three_hundred_Work_Items_in_refinement_still_answer_with_a_verdict_within_two_seconds()
        {
            var team = GivenATeamWithThreeHundredWorkItemsInRefinementRefiningOnTuesdays();

            var (tab, elapsed) = WhenTheTabIsReadASecondTime(team);

            ThenTheVerdictCameBackInTime(tab, elapsed);
        }
    }
}
