using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Staleness
{
    /// <summary>
    /// Lighthouse says, on its own WIP reads, which Work Items in progress are stale
    /// and why - one rule for the dashboard, `lh` and the MCP tools alike.
    /// Persona: flow-coach (and the client user reading `lh metrics team --metrics wip`).
    /// Driving ports: the Team and Portfolio WIP reads, with the day they are asked about.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6247")]
    [Category("slice-01")]
    public partial class Slice01ServerMarksStaleTest : StaleWorkItemsAcceptanceTest
    {
        // @walking_skeleton @driving_port @real-io @contract-shape:unbounded-preservation
        [Test]
        public async Task A_flow_coach_reads_which_Work_Item_has_sat_too_long_in_Review_and_why()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-12", Review, days: 12);
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-2", InProgress, days: 2);
            var storedBefore = GivenWhatLighthouseHasStored();

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemIsStaleBecause(read, "STORY-12", TimeInState(12, Review));
            ThenTheWorkItemIsNotStale(read, "STORY-2");
            ThenTheReadLeftWhatLighthouseHasStoredUnchanged(storedBefore);
        }

        // @driving_port @real-io @contract-shape:pure-function
        [Test]
        public async Task A_flow_coach_reading_todays_work_in_progress_sees_which_Work_Item_has_sat_too_long()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenAWorkItemHasBeenInItsStateUntilTodayFor(team, "STORY-12", Review, days: 12);
            GivenAWorkItemHasBeenInItsStateUntilTodayFor(team, "STORY-2", InProgress, days: 2);

            var read = await WhenTheTeamsWorkInProgressIsReadForToday(team);

            ThenTheWorkItemIsStaleBecause(read, "STORY-12", TimeInState(12, Review));
            ThenTheWorkItemIsNotStale(read, "STORY-2");
        }

        // @driving_port @real-io @contract-shape:pure-function
        [Test]
        public async Task A_Work_Item_is_judged_in_the_state_it_held_on_the_day_asked_about_not_the_one_it_holds_now()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenAWorkItemThatHadBeenInAStateForDaysOnTheDayAskedAboutAndMovedOnTheDayAfter(team, "STORY-1", InProgress, days: 12, movedTo: Review);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemIsStaleBecause(read, "STORY-1", TimeInState(12, InProgress));
        }

        // @driving_port @boundary @contract-shape:pure-function
        [TestCase(5, false)]
        [TestCase(6, true)]
        public async Task A_Work_Item_goes_stale_once_it_has_sat_in_its_state_longer_than_the_threshold(int daysInState, bool expectedStale)
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", Review, daysInState);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemReads(read, "STORY-1", expectedStale, expectedStale ? [TimeInState(daysInState, Review)] : []);
        }

        // @driving_port @boundary @contract-shape:pure-function
        [TestCase(2, false)]
        [TestCase(3, true)]
        public async Task A_Blocked_Work_Item_goes_stale_on_the_day_it_reaches_the_blocked_threshold(int daysBlocked, bool expectedStale)
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", BlockedState, daysBlocked);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemReads(read, "STORY-1", expectedStale, expectedStale ? [BlockedFor(daysBlocked)] : []);
        }

        // @driving_port @error @contract-shape:pure-function
        [Test]
        public async Task A_Blocked_Work_Item_under_the_blocked_threshold_is_not_stale_however_long_it_has_sat_in_its_state()
        {
            var team = GivenATeamWithThresholds(new StalenessThresholds(TimeInStateDays: 5, BlockedDays: 3));
            var item = GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", InProgress, days: 9);
            GivenTheWorkItemHasBeenBlockedFor(item, days: 2);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemReads(read, "STORY-1", expectedStale: false, [TimeInStateWhileBlocked(9, InProgress)]);
        }

        // @driving_port @contract-shape:pure-function
        [Test]
        public async Task A_Blocked_Work_Item_is_stale_for_its_blocked_time_and_still_shows_how_long_it_has_sat_in_its_state()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            var item = GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", InProgress, days: 12);
            GivenTheWorkItemHasBeenBlockedFor(item, days: 4);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemIsStaleBecause(read, "STORY-1", BlockedFor(4), TimeInStateWhileBlocked(12, InProgress));
        }

        // @driving_port @error @contract-shape:pure-function
        [Test]
        public async Task A_Work_Item_still_Blocked_in_its_state_is_not_stale_before_the_blocked_threshold_even_past_the_time_in_state_one()
        {
            var team = GivenATeamWithThresholds(new StalenessThresholds(TimeInStateDays: 5, BlockedDays: 10));
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", BlockedState, days: 7);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemReads(read, "STORY-1", expectedStale: false, [TimeInStateWhileBlocked(7, BlockedState)]);
        }

        // @driving_port @error @contract-shape:pure-function
        [Test]
        public async Task With_both_thresholds_off_no_Work_Item_is_stale_and_none_carries_a_reason()
        {
            var team = GivenATeamWithThresholds(new StalenessThresholds(TimeInStateDays: 0, BlockedDays: 0));
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", InProgress, days: 30);
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-2", BlockedState, days: 30);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemIsNotStale(read, "STORY-1");
            ThenTheWorkItemIsNotStale(read, "STORY-2");
        }

        // @driving_port @error @contract-shape:pure-function
        [Test]
        public async Task Turning_the_time_in_state_trigger_off_leaves_the_blocked_trigger_working()
        {
            var team = GivenATeamWithThresholds(new StalenessThresholds(TimeInStateDays: 0, BlockedDays: 3));
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", InProgress, days: 30);
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-2", BlockedState, days: 3);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemIsNotStale(read, "STORY-1");
            ThenTheWorkItemIsStaleBecause(read, "STORY-2", BlockedFor(3));
        }

        // @driving_port @error @contract-shape:pure-function
        [Test]
        public async Task Turning_the_blocked_trigger_off_leaves_the_time_in_state_trigger_working()
        {
            var team = GivenATeamWithThresholds(new StalenessThresholds(TimeInStateDays: 5, BlockedDays: 0));
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", InProgress, days: 6);
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-2", BlockedState, days: 30);

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemIsStaleBecause(read, "STORY-1", TimeInState(6, InProgress));
            ThenTheWorkItemReads(read, "STORY-2", expectedStale: false, [TimeInStateWhileBlocked(30, BlockedState)]);
        }

        // @driving_port @contract-shape:pure-function
        // Chained: the same Work Item, read as of two consecutive days.
        [Test]
        public async Task A_Work_Item_is_judged_as_of_the_day_the_read_asks_about_not_today()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", Review, days: 5);

            var onTheFifthDay = await WhenTheTeamsWorkInProgressIsReadAsOf(team, AsOf);
            var onTheSixthDay = await WhenTheTeamsWorkInProgressIsReadAsOf(team, AsOf.AddDays(1));

            ThenTheWorkItemIsNotStale(onTheFifthDay, "STORY-1");
            ThenTheWorkItemIsStaleBecause(onTheSixthDay, "STORY-1", TimeInState(6, Review));
        }

        // @driving_port @edge @contract-shape:pure-function
        [Test]
        public async Task A_Work_Item_whose_state_entry_Lighthouse_never_saw_is_not_stale()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenAWorkItemWithNoRecordOfWhenItEnteredItsState(team, "STORY-1");

            var read = await WhenTheTeamsWorkInProgressIsRead(team);

            ThenTheWorkItemIsNotStale(read, "STORY-1");
        }

        // @driving_port @boundary @contract-shape:pure-function
        [TestCase(5, false)]
        [TestCase(6, true)]
        public async Task A_Feature_goes_stale_once_it_has_sat_in_its_state_longer_than_the_Portfolios_threshold(int daysInState, bool expectedStale)
        {
            var portfolio = GivenAPortfolioWithThresholds(new StalenessThresholds(TimeInStateDays: 5, BlockedDays: 3));
            GivenAFeatureHasBeenInItsStateFor(portfolio, "EPIC-1", InProgress, daysInState);

            var read = await WhenThePortfoliosWorkInProgressIsRead(portfolio);

            ThenTheWorkItemReads(read, "EPIC-1", expectedStale, expectedStale ? [TimeInState(daysInState, InProgress)] : []);
        }

        // @driving_port @contract-shape:pure-function
        [Test]
        public async Task A_Blocked_Feature_is_stale_for_its_blocked_time_against_the_Portfolios_blocked_threshold()
        {
            var portfolio = GivenAPortfolioWithThresholds(new StalenessThresholds(TimeInStateDays: 5, BlockedDays: 3));
            var feature = GivenAFeatureHasBeenInItsStateFor(portfolio, "EPIC-1", InProgress, days: 12);
            GivenTheFeatureHasBeenBlockedInThePortfolioFor(feature, portfolio, days: 3);

            var read = await WhenThePortfoliosWorkInProgressIsRead(portfolio);

            ThenTheWorkItemIsStaleBecause(read, "EPIC-1", BlockedFor(3), TimeInStateWhileBlocked(12, InProgress));
        }

        // @driving_port @real-io @contract-shape:pure-function
        // Read for today, so the blocked rule decides and the open blocked spell says since when.
        [Test]
        public async Task A_Feature_Blocked_today_is_stale_for_the_days_since_its_open_blocked_spell_began()
        {
            var portfolio = GivenAPortfolioWithThresholds(new StalenessThresholds(TimeInStateDays: 5, BlockedDays: 3));
            var feature = GivenAFeatureHasBeenInItsStateUntilTodayFor(portfolio, "EPIC-1", BlockedState, days: 12);
            GivenTheFeatureHasBeenBlockedInThePortfolioUntilTodayFor(feature, portfolio, days: 4);

            var read = await WhenThePortfoliosWorkInProgressIsReadForToday(portfolio);

            ThenTheWorkItemIsStaleBecause(read, "EPIC-1", BlockedFor(4), TimeInStateWhileBlocked(12, BlockedState));
        }

        // @driving_port @error @contract-shape:unbounded-preservation
        // Green at hand-off on purpose: it holds today, and it is what keeps a read that never judges
        // staleness from starting to claim "not stale".
        [Test]
        public async Task The_Blocked_items_read_says_nothing_about_staleness()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            var item = GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", InProgress, days: 12);
            GivenTheWorkItemHasBeenBlockedFor(item, days: 4);
            var storedBefore = GivenWhatLighthouseHasStored();

            var read = await WhenTheTeamsBlockedItemsAreReadForTheDay(team);

            ThenTheReadSaysNothingAboutStaleness(read, "STORY-1");
            ThenTheReadLeftWhatLighthouseHasStoredUnchanged(storedBefore);
        }

        // @driving_port @edge @contract-shape:pure-function
        // Pins today's behaviour, which this story does not change: without a day, the read answers
        // for no day at all.
        [Test]
        public async Task A_WIP_read_without_a_day_answers_with_no_Work_Items()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenAWorkItemHasBeenInItsStateFor(team, "STORY-1", Review, days: 12);

            var read = await WhenTheTeamsWorkInProgressIsReadWithoutADay(team);

            ThenTheReadHasNoWorkItems(read);
        }
    }
}
