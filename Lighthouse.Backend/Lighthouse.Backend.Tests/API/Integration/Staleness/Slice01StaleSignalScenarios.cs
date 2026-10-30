using Lighthouse.Backend.Tests.API.Integration.FasterUpdates;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Staleness
{
    /// <summary>
    /// The signal a refresh raises when a Work Item goes stale follows the same rule
    /// the WIP reads answer with. Nothing subscribes to the signal yet, so the signal itself is the
    /// observable. Driving port: the scheduled Team refresh, run by the production update queue.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-6247")]
    [Category("slice-01")]
    public partial class Slice01StaleSignalTest : FasterUpdatesAcceptanceTest
    {
        // Chained: the second refresh finds the same Work Item still stale and says nothing new.
        [Test]
        public async Task A_refresh_signals_a_Work_Item_going_stale_once_with_the_threshold_it_crossed()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            var item = GivenTheTeamHasAWorkItemThatHasBeenInItsStateFor(team, "ITEM-1", InProgress, days: 6);

            await WhenTheScheduledRefreshRuns(team);
            ThenTheWorkItemWasSignalledAsStale(item, thresholdDays: 5);

            await WhenTheScheduledRefreshRuns(team);
            ThenNoWorkItemWasSignalledAsStale();
        }

        [Test]
        public async Task A_refresh_does_not_signal_a_Work_Item_that_has_sat_exactly_as_long_as_the_threshold()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            GivenTheTeamHasAWorkItemThatHasBeenInItsStateFor(team, "ITEM-1", InProgress, days: 5);

            await WhenTheScheduledRefreshRuns(team);

            ThenNoWorkItemWasSignalledAsStale();
        }

        [Test]
        public async Task A_refresh_does_not_signal_a_Blocked_Work_Item_still_under_the_blocked_threshold()
        {
            var team = GivenATeamWithThresholds(timeInStateDays: 5, blockedDays: 10);
            GivenTheTeamHasAWorkItemThatHasBeenInItsStateFor(team, "ITEM-1", BlockedState, days: 7);

            await WhenTheScheduledRefreshRuns(team);

            ThenNoWorkItemWasSignalledAsStale();
        }

        [Test]
        public async Task A_refresh_signals_a_Blocked_Work_Item_with_the_blocked_threshold_it_crossed()
        {
            var team = GivenATeamWithThresholds(timeInStateDays: 0, blockedDays: 3);
            var item = GivenTheTeamHasAWorkItemThatHasBeenInItsStateFor(team, "ITEM-1", BlockedState, days: 3);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheWorkItemWasSignalledAsStale(item, thresholdDays: 3);
        }

        // With both triggers on, the threshold named is the blocked one, because that is the reason
        // that made the Work Item stale.
        [Test]
        public async Task A_refresh_names_the_blocked_threshold_when_a_Blocked_Work_Item_goes_stale_with_both_thresholds_set()
        {
            var team = GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked();
            var item = GivenTheTeamHasAWorkItemThatHasBeenInItsStateFor(team, "ITEM-1", BlockedState, days: 3);

            await WhenTheScheduledRefreshRuns(team);

            ThenTheWorkItemWasSignalledAsStale(item, thresholdDays: 3);
        }

        [Test]
        public async Task A_refresh_never_signals_staleness_while_both_thresholds_are_off()
        {
            var team = GivenATeamWithThresholds(timeInStateDays: 0, blockedDays: 0);
            GivenTheTeamHasAWorkItemThatHasBeenInItsStateFor(team, "ITEM-1", InProgress, days: 30);
            GivenTheTeamHasAWorkItemThatHasBeenInItsStateFor(team, "ITEM-2", BlockedState, days: 30);

            await WhenTheScheduledRefreshRuns(team);

            ThenNoWorkItemWasSignalledAsStale();
        }
    }
}
