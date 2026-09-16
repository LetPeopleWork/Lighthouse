using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #5511 — Task Manager), slice 04 / ADO #5842: stop a refresh that
    /// is doing harm. Driving port: the System-Administrator-guarded task list and its cancel route on
    /// <c>UpdateController</c>, exercised over HTTP. US-04, AC-04.1 … AC-04.8.
    ///
    /// The thing an operator is buying here is the ability to intervene, so the scenarios are about what
    /// stops and what does not: a queued refresh must never reach the tracker at all, a running one must
    /// stop without stranding what it staged, and neither may take anything else down with it.
    ///
    /// Answered elsewhere, none of it silently:
    ///
    /// AC-04.2's granularity is a measurement, not an assertion — it is recorded in the slice brief from
    /// the probe, and the mechanism it rests on is pinned by <c>Slice04CancellationReachProbe</c> (an
    /// ambient value set by the queue is readable inside a connector call) and by the paging tests in each
    /// connector. A ratio between two durations asserted in a suite becomes a flake, not a guard.
    ///
    /// AC-04.3's ordinals are frozen in <c>UpdateProgressOrdinalFreezeTest</c>, because the claim is about
    /// numbers already written into the Redis hash rather than about anything this endpoint answers.
    ///
    /// Cancelling work running on another replica is <c>TaskManagerCancellationMultiReplicaTests</c>: it
    /// needs two stores on one Redis, which is not something a single test host can stand in for.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-04")]
    public partial class Slice04StopARefreshTest
    {
        // @walking_skeleton @driving_port @real-io @AC-04.1 — the whole promise in one scenario: an
        // operator presses Cancel and the tracker is never contacted. Work that has not started is the
        // case where "stopped" can mean something absolute rather than best-effort.
        [Test]
        public async Task A_refresh_that_is_still_waiting_is_cancelled_without_the_tracker_ever_being_asked()
        {
            var running = GivenATeamThatIsRefreshedOnSchedule();
            var waiting = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(running);
            await WhenARefreshOfThatTeamIsAlsoAskedFor(waiting);
            await WhenTheOperatorCancels(waiting);

            await ThenTheTrackerWasNeverAskedAbout(waiting);
            await ThenTheTaskListDoesNotMention(waiting);
        }

        // @driving_port @real-io @AC-04.1 — *when* the row goes, not just that it does. The scenario above
        // releases the refresh ahead of it and drains the queue before it looks, so it passes just as well
        // against a cancel nobody acts on until the reader eventually reaches the row — which is what the
        // maintainer met: press Cancel, and the work sits there until whatever is running finishes. The
        // queue is one sequential reader, so "eventually" is as long as the refresh ahead of it takes.
        [Test]
        public async Task A_refresh_cancelled_while_it_waits_leaves_the_list_without_waiting_its_turn()
        {
            var running = GivenATeamThatIsRefreshedOnSchedule();
            var waiting = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(running);
            await WhenARefreshOfThatTeamIsAlsoAskedFor(waiting);
            await WhenTheOperatorCancels(waiting);

            await ThenTheTaskListAlreadyDoesNotMention(waiting);
            await ThenThatTeamIsStillOnTheTaskList(running);
        }

        // @driving_port @real-io @AC-04.2 — a running refresh stops rather than running to completion. The
        // interval is the subject of the probe's measurement; what this pins is that it stops at all.
        [Test]
        public async Task A_refresh_that_is_running_stops_instead_of_running_to_completion()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            await WhenTheOperatorCancels(team);

            await ThenThatRefreshStopsBeforeItHasReadEveryPage(team);
        }

        // @driving_port @real-io @AC-04.3 — Cancelled is the last word the browser hears, and it is reached
        // by the same monotonic advance every other terminal state is. A cancel that left the row saying
        // "running" would be a button with no feedback, which reads as a button that did not work.
        [Test]
        public async Task A_cancelled_refresh_tells_the_browser_it_was_cancelled()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            await WhenTheOperatorCancels(team);

            await ThenTheBrowserWasToldThatRefreshWasCancelled(team);
        }

        // @driving_port @real-io @error @AC-04.4 — the invariant that is easy to miss, reached through a
        // door slice 01 did not use. A round that never finishes silently drops every write it staged, so a
        // cancelled run owes its round the same report a run that finished does.
        [Test]
        public async Task A_cancelled_refresh_still_finishes_the_write_back_round_it_opened()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            await WhenTheOperatorCancels(team);

            await ThenTheWriteBackRoundWasAccountedFor();
        }

        // @driving_port @real-io @error @AC-04.4 — the other half. Work parked behind a cancelled key waits
        // on that key leaving the queue, not on it leaving successfully; held work stranded by a cancel
        // stays parked until something unrelated happens to poke the same key.
        [Test]
        public async Task A_cancelled_refresh_still_lets_go_of_the_work_held_behind_it()
        {
            var cancelled = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(cancelled);
            GivenSomethingIsHeldBehind(cancelled);

            await WhenTheOperatorCancels(cancelled);

            await ThenWhatWasHeldBehindItWasLetGo();
        }

        // @driving_port @real-io @error @AC-04.5 — a cancel must leave a state a later refresh corrects.
        // Stopping mid-flight and leaving a half-written entity that nothing overwrites would trade a
        // runaway refresh for a quietly wrong one, which is the worse of the two.
        [Test]
        public async Task A_refresh_that_runs_after_a_cancelled_one_finishes_normally()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            await WhenTheOperatorCancels(team);
            GivenTheTrackerAnswersNormallyAgain();

            await ThenAFreshRefreshOfThatTeamCompletes(team);
        }

        // @driving_port @real-io @error @AC-04.6 — the row was drawn before the operator clicked it, so
        // "it finished while you were reading" is the ordinary case. Answering an error would put a failure
        // in front of somebody who did nothing wrong.
        [Test]
        public async Task Cancelling_something_that_has_already_finished_is_accepted_and_changes_nothing()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            await ThenCancellingIsAcceptedFor(team);
            await ThenCancellingIsAcceptedFor(team);
            await ThenTheTaskListIsAnsweredAndEmpty();
        }

        // @driving_port @real-io @error — the one thing the route does not accept. A removal is listed with
        // a Stop control beside it like everything else, so this is a button an operator can genuinely
        // press; stopping a delete half-way would tell the caller waiting on it that the entity had gone
        // while its row is still in the database.
        [Test]
        public async Task Asking_to_stop_a_removal_is_refused_and_says_why()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            await ThenCancellingARemovalOfThatTeamIsRefusedWithAReasonToShow(team);
        }

        // @driving_port @real-io @error @AC-04.6 — the same refusal the list it is reached from gives. A
        // cancel route that is guarded more loosely than the list would let somebody stop every refresh on
        // an instance they cannot even see.
        [Test]
        public async Task The_cancel_route_is_refused_to_somebody_who_is_not_a_system_administrator()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            await ThenCancellingRefusesANonAdministratorTheSameWayTheTaskListDoes(team);
        }

        // @driving_port @real-io @AC-04.7 — cancel is per key. An operator stopping one runaway refresh
        // must not stop the instance, or the button is too dangerous to press.
        [Test]
        public async Task Cancelling_one_refresh_leaves_the_others_alone()
        {
            var cancelled = GivenATeamThatIsRefreshedOnSchedule();
            var spared = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerAnswersOnePageAtATimeUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(cancelled);
            await WhenARefreshOfThatTeamIsAlsoAskedFor(spared);

            await WhenTheOperatorCancels(cancelled);

            await ThenThatTeamIsStillOnTheTaskList(spared);
        }
    }
}
