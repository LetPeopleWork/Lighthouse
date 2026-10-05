using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Before the next Refinement the tab says whether to refine more or stop. The number is the Team's own
    /// manual How Many forecast for the working days until the next Refinement: the low end is what the Team
    /// pulls more likely than not (the median), the high end a count only 15% of runs pull more than.
    /// The ready Work Items - by votes on a Team without stage rules, by stage on a Team with them - are
    /// below, in or above that range, ends included in range. The answer is facts only: counts, the range,
    /// its percentiles, the date, the working days and a verdict; the browser and the clients say it in
    /// words. Where no number can be given the tab says why: no cadence, too little Throughput history (the
    /// same guard forecasts use), or no refinement states. The high end is stated as forecast, never cut down
    /// to the Work Items listed: the browser numbers that many rows in whatever order it shows them and draws
    /// the "enough for" line after them, or after the last row when fewer are listed.
    ///
    /// Driving ports: the Team settings write, the vote write, the Refinement tab's read and the manual
    /// forecast. Step definitions live in Slice05NeedAndVerdictSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-05")]
    public partial class Slice05NeedAndVerdictTest : RefinementNeedAcceptanceTest
    {
        // --- The verdict ---

        // @driving_port @real-io @us-05 @slice-05 @kpi-OUT-5510-K3-in-range-on-refinement-day @contract-shape:pure-function
        // The forecast differs at every likelihood, so only the median and the 85% reading give 5 and 8.
        [Test]
        public async Task Below_range_says_how_many_are_ready_against_the_range_the_Team_is_likely_to_pull()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            GivenTheTeamIsLikelyToPullFiveToEightBeforeThursday();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNeedIs(tab, new NeedReading(Below, null, 5, 8, 50, 85, 6));
            ThenTheNextRefinementIsThursdayTheEighthWithTwoReady(tab);
        }

        // @driving_port @real-io @us-05 @slice-05 @boundary @kpi-OUT-5510-K3-in-range-on-refinement-day @contract-shape:pure-function
        // Two Work Items are ready; both ends of the range belong to it.
        [TestCase(3, 8, Below)]
        [TestCase(2, 8, InRange)]
        [TestCase(1, 2, InRange)]
        [TestCase(0, 1, Above)]
        public async Task The_verdict_compares_the_ready_count_with_both_ends_of_the_range(int low, int high, string verdict)
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            TheTeamIsLikelyToPull(6, (50, low), (85, high));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheVerdictIs(tab, verdict);
        }

        // @driving_port @real-io @us-05 @us-13 @slice-05 @contract-shape:bounded-change
        // Without stage rules the votes make Work Items Ready, and so they move the verdict.
        [Test]
        public async Task On_a_Team_without_stages_a_vote_that_makes_a_Work_Item_Ready_moves_the_verdict()
        {
            var gravity = await GivenGravityWithoutStagesRefinesOnThursdayTheEighthAndIsLikelyToPullOneToThree();
            ThenTheVerdictIs(await WhenTheCoachOpensTheRefinementTab(gravity), Below);

            await WhenThreeVotersSayYesOn(gravity, ConfigurationManagement);

            ThenTheVerdictIs(await WhenTheCoachOpensTheRefinementTab(gravity), InRange);
        }

        // @driving_port @real-io @us-05 @us-03 @slice-05 @boundary @contract-shape:bounded-change
        // With stage rules the tracker decides what is ready; votes are shown, not counted.
        [Test]
        public async Task On_a_Team_with_stages_votes_do_not_move_the_verdict()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            TheTeamIsLikelyToPull(6, (50, 3), (85, 5));

            await WhenThreeVotersSayYesOn(gravity, ConfigurationManagement);

            ThenTheVerdictIs(await WhenTheCoachOpensTheRefinementTab(gravity), Below);
        }

        // --- The number behind it ---

        // @driving_port @real-io @us-05 @slice-05 @contract-shape:pure-function
        // A Team that finishes two Work Items every day is forecast twelve over six working days, whatever the
        // engine draws, so both reads of the shipped engine must agree exactly. With so even a history every
        // likelihood reads twelve: this proves the horizon, and the band's own tests prove which likelihood
        // each end is read at.
        [Test]
        public async Task The_range_uses_the_same_horizon_as_the_manual_forecast()
        {
            var gravity = await GivenGravityFinishesTwoADayAndRefinesOnTuesdays();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            await ThenTheRangeUsesTheManualForecastsHorizonToTuesdayTheThirteenth(tab, gravity);
        }

        // @driving_port @real-io @us-05 @slice-05 @boundary @contract-shape:pure-function
        // Monday 5 October is a blackout day, so only five of the six days to Thursday are working days.
        [Test]
        public async Task Blackout_days_before_the_next_Refinement_are_not_counted()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            await ABlackoutDayOn(new DateOnly(2026, 10, 5));
            TheTeamIsLikelyToPull(5, (50, 4), (85, 7));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIs(tab, low: 4, high: 7, horizonWorkingDays: 5);
        }

        // @driving_port @real-io @us-05 @us-04 @slice-05 @boundary @kpi-OUT-5510-K3-in-range-on-refinement-day @contract-shape:pure-function
        // On a Refinement day today's session is the moment to top up, so the count looks a week ahead.
        [Test]
        public async Task On_a_Refinement_day_the_number_is_for_the_following_Refinement()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            TodayIs(2026, 10, 8);
            TheTeamIsLikelyToPull(7, (50, 6), (85, 9));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIs(tab, low: 6, high: 9, horizonWorkingDays: 7);
        }

        // --- When there is no number ---

        // @driving_port @real-io @us-05 @slice-05 @error @contract-shape:pure-function
        // The guard forecasts use: at least five days with finished Work Items.
        [Test]
        public async Task Too_little_Throughput_history_gives_the_familiar_guard_instead_of_a_number()
        {
            var gravity = await GivenGravityFinishedWorkOnOnlyFourDaysAndRefinesOnThursdays();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenThereIsNoNumberBecause(tab, InsufficientData);
        }

        // @driving_port @real-io @us-05 @us-04 @slice-05 @error @contract-shape:pure-function
        [Test]
        public async Task Without_a_cadence_there_is_no_number_and_the_list_stays()
        {
            var gravity = await GivenGravityHasTwoReadyWithoutACadence();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenThereIsNoNumberBecause(tab, NoCadence);
            ThenAllSixAreStillListed(tab);
        }

        // @driving_port @real-io @us-05 @us-01 @slice-05 @error @contract-shape:pure-function
        [Test]
        public async Task A_Team_nobody_set_up_has_no_number_because_it_has_no_refinement_states()
        {
            var zenith = GivenATeamWithoutRefinementStates();

            var tab = await WhenTheCoachOpensTheRefinementTab(zenith);

            ThenThereIsNoNumberBecause(tab, NoRefinementStates);
        }

        // --- How many rows are needed ---

        // @driving_port @real-io @us-06 @slice-05 @boundary @contract-shape:pure-function
        // Gravity lists six Work Items. The browser needs the true high end to say that all six are needed,
        // so the answer never cuts it down to what is listed.
        [TestCase(3)]
        [TestCase(6)]
        [TestCase(11)]
        public async Task The_high_end_is_stated_as_forecast_whatever_number_of_Work_Items_is_listed(int high)
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            TheTeamIsLikelyToPull(6, (50, 1), (85, high));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheHighEndIsAndSixAreListed(tab, high);
        }

        // @driving_port @real-io @us-06 @slice-05 @boundary @contract-shape:pure-function
        // A Team likely to pull nothing at all before the next Refinement needs nothing refined.
        [Test]
        public async Task A_range_of_nothing_says_stop_with_two_ready()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdayTheEighth();
            TheTeamIsLikelyToPull(6, (50, 0), (85, 0));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNeedIs(tab, new NeedReading(Above, null, 0, 0, 50, 85, 6));
        }

        // --- Guardrail ---

        // @driving_port @real-io @us-05 @slice-05 @boundary @kpi-OUT-5510-K2-refinement-tab-weekly @contract-shape:pure-function
        // Second, warm read; the shipped forecast engine runs.
        [Test]
        public async Task Three_hundred_Work_Items_in_refinement_still_answer_with_a_verdict_within_two_seconds()
        {
            var team = await GivenATeamWithThreeHundredWorkItemsInRefinementRefiningOnTuesdays();

            var (tab, elapsed) = await WhenTheTabIsReadASecondTime(team);

            ThenTheVerdictCameBackInTime(tab, elapsed);
        }
    }
}
