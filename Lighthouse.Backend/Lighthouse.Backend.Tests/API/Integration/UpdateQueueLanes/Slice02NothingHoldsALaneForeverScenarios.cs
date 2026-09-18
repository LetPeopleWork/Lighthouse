using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.UpdateQueueLanes
{
    /// <summary>
    /// DISTILL acceptance scenarios (ADO #5877), slice 02: nothing holds a lane forever. Driving ports:
    /// a refresh triggered on an updater and run by the production update queue, the task list and its
    /// cancel route, the refresh history, and the instance's configuration. US-02, AC-02.1 … AC-02.8.
    ///
    /// Every scenario here is <c>[Ignore]</c>d and the suite is green at hand-off; DELIVER unskips them
    /// one at a time as the bound is built.
    ///
    /// The bound is armed from the instance's own <see cref="TimeProvider"/>, so these scenarios move a
    /// fake clock rather than wait. A scenario that waited out a real bound would either take the bound
    /// or take three hours, and the first is a bound nobody would configure.
    ///
    /// Answered elsewhere, neither silently:
    ///
    /// AC-02.4 asks that a bound-ended run stops within one page round-trip. What is asserted here is
    /// that it stops at a page boundary at all — the page count stops moving rather than running on to
    /// the end. The interval itself is a measurement against a real connector, and this repository has
    /// already learned once that a ratio between two durations asserted in a suite becomes a flake
    /// rather than a guard; it is AC-02.8's number, taken on Tenant Zero.
    ///
    /// AC-02.8 is a production-data criterion: on Tenant Zero with the bound temporarily lowered, a
    /// real refresh against a real connector is ended by it, the history records it, the lane frees and
    /// the next scheduled refresh of that type runs normally, bound restored afterwards. It is a
    /// dogfood check rather than a test, and it is recorded as one in the feature delta.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("story-5877-update-queue-lanes")]
    [Category("slice-02")]
    public partial class Slice02NothingHoldsALaneForeverTest
    {
        private const string Pending = "pending — DELIVER unskips this as slice 02 is built";

        // @driving_port @real-io @AC-02.1 — the whole promise in one scenario. Nobody pressed anything
        // and the run is over; today it would still be going, and the only thing that ends it is a
        // restart of the instance.
        [Test]
        [Ignore(Pending)]
        public async Task A_refresh_that_outruns_the_bound_is_stopped_without_anybody_pressing_anything()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(AGenerousBoundInMinutes);
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            WhenTheRunHasBeenGoingLongerThanTheBoundAllows();

            await ThenThatTeamRefreshEndsWithoutAnybodyAskingItTo(team);
        }

        // @driving_port @real-io @AC-02.2 — the operative clause. An operator who pressed nothing must
        // not read a history that says they did; a record that says something they know is untrue is a
        // record they stop believing about everything else too.
        [Test]
        [Ignore(Pending)]
        public async Task The_refresh_history_says_the_instance_stopped_the_run_and_not_the_operator()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(AGenerousBoundInMinutes);
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            WhenTheRunHasBeenGoingLongerThanTheBoundAllows();
            await WhenThatRefreshHasFinishedOneWayOrAnother(team);

            await ThenTheRecordedRunNamesTheBoundAsWhatEndedIt(team);
        }

        // @driving_port @real-io @AC-02.2 — the other direction, and the one a derived answer gets right
        // where a flag written at stop time does not. Two stops can land in the same instant; the
        // operator pressed one of them and the history has to say so.
        [Test]
        [Ignore(Pending)]
        public async Task An_operator_who_stopped_a_run_is_still_recorded_as_the_one_who_stopped_it()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(AGenerousBoundInMinutes);
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            await WhenTheOperatorStops(KeyOf(team));
            await WhenThatRefreshHasFinishedOneWayOrAnother(team);

            await ThenTheRecordedRunDoesNotBlameTheBound(team);
        }

        // @driving_port @real-io @AC-02.3 — a bound that stopped the run and left the lane held would
        // have replaced one wedged instance with another.
        [Test]
        [Ignore(Pending)]
        public async Task The_lane_is_free_afterwards_and_the_next_refresh_of_that_type_starts()
        {
            var stopped = GivenATeamThatIsRefreshedOnSchedule();
            var next = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(AGenerousBoundInMinutes);
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(stopped);
            await WhenARefreshOfThatTeamIsAlsoAskedFor(next);
            WhenTheRunHasBeenGoingLongerThanTheBoundAllows();

            await ThenThatTeamRefreshGetsGoing(next);
        }

        // @driving_port @real-io @AC-02.4 — the slice's own hypothesis. If the bound fires and the
        // refresh pages on regardless, the bound is a log line rather than a lever and the slice has
        // failed on its own terms rather than quietly shipping a promise it cannot keep.
        [Test]
        [Ignore(Pending)]
        public async Task A_refresh_ended_by_the_bound_stops_at_its_next_page_rather_than_running_on()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(AGenerousBoundInMinutes);
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            WhenTheRunHasBeenGoingLongerThanTheBoundAllows();

            await ThenThatRefreshStopsAskingTheTrackerForPages(team);
        }

        // @driving_port @real-io @AC-02.5 — the bound is per run. A follow-up that inherited the elapsed
        // time of the run it follows would be killed the instant it started, and every refresh of a slow
        // entity after the first would die on arrival — which makes the reported symptom worse.
        [Test]
        [Ignore(Pending)]
        public async Task The_follow_up_of_a_bound_ended_refresh_runs_on_a_clock_of_its_own()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(AGenerousBoundInMinutes);
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            GivenAnotherRefreshOfThatTeamIsAskedForWhileTheFirstIsStillGoing(team);
            WhenTheRunHasBeenGoingLongerThanTheBoundAllows();

            await ThenTheFollowUpRunsToCompletion(team);
        }

        // @driving_port @real-io @error @AC-02.6 — the fallback that matters most is the one that is not
        // zero. A bound read as "no time at all" turns every refresh on the instance into a refresh that
        // is killed the moment it starts, which is a far worse instance than the one being fixed.
        [Test]
        [Ignore(Pending)]
        public async Task A_run_limit_nobody_could_have_meant_falls_back_to_the_default_rather_than_to_no_time_at_all()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(SomethingNobodyCouldHaveMeant);
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);

            await ThenThatTeamRefreshIsNotStoppedOutOfHand(team);
        }

        // @driving_port @real-io @error @AC-02.6 — the read half, over every way the value can be
        // recorded. The instance answers the bound it will actually use, so an operator who has typed
        // something it cannot use finds out by reading the field rather than by watching refreshes.
        // DELIVER adds the route before unskipping this: the configuration surface is part of the slice.
        [Test]
        [Ignore(Pending)]
        [TestCaseSource(nameof(EveryWayARunLimitCanBeRecorded))]
        public async Task The_instance_answers_the_run_limit_it_will_actually_use(string? recorded, int minutesUsed)
        {
            GivenTheRunLimitIsRecordedAsWhateverAnOperatorLeftBehind(recorded);

            await ThenTheInstanceSaysItsRunLimitIs(minutesUsed);
        }

        // @driving_port @real-io @error @AC-02.7 — removals are exempt, and structurally so. They are
        // already refused by the cancel route; a bound that made them evictable behind the operator's
        // back would tell the caller waiting on a removal that the entity has gone while its row is
        // still in the database.
        [Test]
        [Ignore(Pending)]
        public async Task A_removal_is_never_ended_by_the_bound()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(AGenerousBoundInMinutes);

            GivenARemovalOfThatTeamIsUnderWayAndWillNotFinishUntilWeSaySo(team);
            WhenTheRunHasBeenGoingLongerThanTheBoundAllows();

            await ThenThatRemovalIsStillGoing(team);
        }

        // @driving_port @real-io @AC-02.1 — an instance that ends its own run is the one event in this
        // story an operator most wants to find afterwards, and the existing cancel line reads as
        // somebody having pressed something and is below the level the recent-problems popover shows.
        [Test]
        [Ignore(Pending)]
        public async Task An_instance_that_ends_its_own_run_says_so_where_an_operator_looks_for_problems()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheInstanceRunLimitIsRecordedAs(AGenerousBoundInMinutes);
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            WhenTheRunHasBeenGoingLongerThanTheBoundAllows();
            await WhenThatRefreshHasFinishedOneWayOrAnother(team);

            ThenTheInstanceWarnedThatItEndedThatRefreshItself(team);
        }
    }
}
