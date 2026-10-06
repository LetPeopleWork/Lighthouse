using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// A Refinement has to leave enough ready to last until the Refinement after it, so the need covers one
    /// Refinement cycle: the working days after the next Refinement up to and including the one after it.
    /// When today is a Refinement day, the cycle runs from today to the next Refinement. Which day of the
    /// week it is does not change the number. A Team refining on several weekdays plans for the gap between
    /// its next two Refinements, so the number follows the gap. Blackout days inside the cycle are not working
    /// days; a Refinement on a blackout day does not happen, so the cycle runs to the next one that does. The
    /// need names the cycle it covers, its first day and its last; without a number it names neither. The
    /// heading still names the next Refinement, and the ready count is unchanged.
    ///
    /// Driving ports: the Team settings write, the blackout-day writes and the Refinement tab's read. Step
    /// definitions live in NeedOverOneCycleSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("need-over-one-cycle")]
    public partial class NeedOverOneCycleTest : RefinementNeedAcceptanceTest
    {
        // --- One cycle ---

        // @walking_skeleton @driving_port @real-io @need-over-one-cycle @contract-shape:pure-function
        // Friday 2 October: the next Refinement is Thursday the 8th and the one after it Thursday the 15th.
        [Test]
        public async Task The_need_covers_the_working_days_from_the_next_Refinement_to_the_one_after()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNeedIsBelowFiveToEightOverTheCycle(tab, ThursdayTheEighth, ThursdayTheFifteenth);
            ThenTheHeadingStillNamesTheNextRefinement(tab, ThursdayTheEighth, isRefinementDay: false);
        }

        // @driving_port @real-io @need-over-one-cycle @boundary @contract-shape:pure-function
        // Friday, Monday, Tuesday and Wednesday all look to the same cycle, so they all read the same number.
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        public async Task Every_day_before_the_next_Refinement_reads_the_same_number(int todayInOctober)
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            GivenTodayIsOctober(todayInOctober);
            GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNeedIsBelowFiveToEightOverTheCycle(tab, ThursdayTheEighth, ThursdayTheFifteenth);
        }

        // @driving_port @real-io @need-over-one-cycle @boundary @contract-shape:pure-function
        // With weekends blacked out the cycle from Thursday to Thursday has five working days: Friday the 9th
        // and Monday to Thursday the following week.
        [TestCase(5)]
        [TestCase(6)]
        public async Task With_weekends_blacked_out_Monday_and_Tuesday_both_read_the_five_working_days_of_the_cycle(int todayInOctober)
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            await GivenWeekendsAreBlackedOut();
            GivenTodayIsOctober(todayInOctober);
            GivenTheTeamIsLikelyToPull(5, (50, 4), (85, 6));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIsOverTheCycle(tab, low: 4, high: 6, workingDays: 5, ThursdayTheEighth, ThursdayTheFifteenth);
        }

        // @driving_port @real-io @need-over-one-cycle @us-04 @boundary @contract-shape:pure-function
        // Today's session is the moment to top up, so the cycle starts today and runs to the next Refinement.
        [Test]
        public async Task On_a_Refinement_day_the_cycle_runs_from_today_to_the_next_Refinement()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            GivenTodayIsOctober(8);
            GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheNeedIsBelowFiveToEightOverTheCycle(tab, ThursdayTheEighth, ThursdayTheFifteenth);
            ThenTheHeadingStillNamesTheNextRefinement(tab, ThursdayTheFifteenth, isRefinementDay: true);
        }

        // --- Several weekdays ---

        // @driving_port @real-io @need-over-one-cycle @boundary @contract-shape:pure-function
        // Mondays and Thursdays: Monday to Thursday is three days, Thursday to Monday four, so the number
        // follows whichever gap comes next. Gravity finishes one Work Item a day, so the shipped forecast
        // reads one per working day at both ends.
        [TestCase(2, MondayTheFifth, ThursdayTheEighth, 3)]
        [TestCase(5, MondayTheFifth, ThursdayTheEighth, 3)]
        [TestCase(6, ThursdayTheEighth, MondayTheTwelfth, 4)]
        [TestCase(8, ThursdayTheEighth, MondayTheTwelfth, 4)]
        public async Task A_Team_refining_on_two_weekdays_plans_for_the_gap_between_its_next_two_Refinements(
            int todayInOctober, string cycleStart, string cycleEnd, int workingDays)
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnMondaysAndThursdays();
            GivenTodayIsOctober(todayInOctober);

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIsOverTheCycle(tab, low: workingDays, high: workingDays, workingDays, cycleStart, cycleEnd);
        }

        // --- Blackout days ---

        // @driving_port @real-io @need-over-one-cycle @boundary @contract-shape:pure-function
        // Monday the 12th lies inside the cycle and shortens it; Monday the 5th comes before the next
        // Refinement and no longer changes the number.
        [TestCase(12, 6)]
        [TestCase(5, 7)]
        public async Task Only_a_blackout_day_inside_the_cycle_shortens_it(int blackoutDayInOctober, int workingDays)
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            await GivenABlackoutDayOn(new DateOnly(2026, 10, blackoutDayInOctober));
            GivenTheTeamIsLikelyToPull(workingDays, (50, 4), (85, 9));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIsOverTheCycle(tab, low: 4, high: 9, workingDays, ThursdayTheEighth, ThursdayTheFifteenth);
        }

        // @driving_port @real-io @need-over-one-cycle @boundary @contract-shape:pure-function
        // Nobody refines on Thursday the 15th, so the cycle runs on to Thursday the 22nd: fourteen days, one
        // of them blacked out.
        [Test]
        public async Task A_blacked_out_Refinement_after_the_next_is_skipped_and_the_cycle_runs_to_the_one_that_happens()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            await GivenABlackoutDayOn(new DateOnly(2026, 10, 15));
            GivenTheTeamIsLikelyToPull(13, (50, 9), (85, 14));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIsOverTheCycle(tab, low: 9, high: 14, workingDays: 13, ThursdayTheEighth, ThursdayTheTwentySecond);
        }

        // @driving_port @real-io @need-over-one-cycle @us-04 @boundary @contract-shape:pure-function
        // Nobody refines on Thursday the 8th, so the next Refinement is the 15th and the cycle runs to the 22nd.
        [Test]
        public async Task A_blacked_out_next_Refinement_is_skipped_and_the_cycle_starts_at_the_one_that_happens()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            await GivenABlackoutDayOn(new DateOnly(2026, 10, 8));
            GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIsOverTheCycle(tab, low: 5, high: 8, workingDays: 7, ThursdayTheFifteenth, ThursdayTheTwentySecond);
            ThenTheHeadingStillNamesTheNextRefinement(tab, ThursdayTheFifteenth, isRefinementDay: false);
        }

        // @driving_port @real-io @need-over-one-cycle @us-04 @boundary @contract-shape:pure-function
        // A Thursday blacked out is no Refinement day, so the cycle does not start today but at the next one.
        [Test]
        public async Task Today_a_blacked_out_Refinement_day_starts_no_cycle_and_the_next_Refinement_does()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            await GivenABlackoutDayOn(new DateOnly(2026, 10, 8));
            GivenTodayIsOctober(8);
            GivenTheTeamIsLikelyToPullFiveToEightOverItsCycle();

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIsOverTheCycle(tab, low: 5, high: 8, workingDays: 7, ThursdayTheFifteenth, ThursdayTheTwentySecond);
            ThenTheHeadingStillNamesTheNextRefinement(tab, ThursdayTheFifteenth, isRefinementDay: false);
        }

        // @driving_port @real-io @need-over-one-cycle @boundary @contract-shape:pure-function
        // Every day up to Wednesday 29 September 2027 is blacked out, so the next Refinement is Thursday the
        // 30th, almost a year ahead. The one after it would be Thursday 7 October 2027, a blackout day too, so
        // the cycle runs to Thursday 14 October 2027. A Refinement that far ahead still counts as blacked out.
        [Test]
        public async Task A_blackout_on_the_Refinement_after_the_next_is_honoured_even_a_year_ahead()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            await GivenEveryDayIsBlackedOutFrom(new DateOnly(2026, 10, 3), new DateOnly(2027, 9, 29));
            await GivenABlackoutDayOn(new DateOnly(2027, 10, 7));
            GivenTheTeamIsLikelyToPull(13, (50, 9), (85, 14));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenTheRangeIsOverTheCycle(tab, low: 9, high: 14, workingDays: 13, "2027-09-30", "2027-10-14");
        }

        // --- No cycle, no number ---

        // @driving_port @real-io @need-over-one-cycle @us-05 @error @contract-shape:pure-function
        // Thursday the 8th happens, but every Thursday in the year after it is blacked out: there is no
        // Refinement worth planning for, as with no cadence at all. The heading still names the 8th.
        [Test]
        public async Task A_year_of_blacked_out_Refinements_after_the_next_leaves_no_cycle_and_no_number()
        {
            var gravity = await GivenGravityHasTwoReadyAndRefinesOnThursdaysOnFridayTheSecond();
            await GivenEveryDayIsBlackedOutFrom(new DateOnly(2026, 10, 9), new DateOnly(2027, 11, 12));

            var tab = await WhenTheCoachOpensTheRefinementTab(gravity);

            ThenThereIsNoNumberAndNoCycleBecause(tab, NoCadence);
            ThenTheHeadingStillNamesTheNextRefinement(tab, ThursdayTheEighth, isRefinementDay: false);
        }

        // @driving_port @real-io @need-over-one-cycle @us-05 @error @contract-shape:pure-function
        // Whatever is missing, a need without a range names no cycle either.
        [TestCase(MissingSetUp.Cadence, NoCadence)]
        [TestCase(MissingSetUp.ThroughputHistory, InsufficientData)]
        [TestCase(MissingSetUp.RefinementStates, NoRefinementStates)]
        public async Task Without_a_number_the_need_names_no_cycle(MissingSetUp missing, string reason)
        {
            var team = await GivenATeamWithout(missing);

            var tab = await WhenTheCoachOpensTheRefinementTab(team);

            ThenThereIsNoNumberAndNoCycleBecause(tab, reason);
        }
    }
}
