using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #5511 — Task Manager), slice 03 / ADO #5841: how long has it
    /// been going. Driving port: the System-Administrator-guarded task list on <c>UpdateController</c>,
    /// exercised over HTTP, the same one slice 02 established. US-03, AC-03.1 … AC-03.5.
    ///
    /// The question this slice answers is a comparison, not a reading: a refresh that started four
    /// seconds ago and one that has been going for forty minutes are the same row today. So every
    /// scenario here is about a number moving — or deliberately not moving, or deliberately absent.
    ///
    /// The instance clock is pinned for the whole fixture. Elapsed time asserted against real wall clock
    /// could only ever be "greater than zero", which is satisfied by a stopwatch started at the wrong
    /// moment; with the clock pinned, the assertions are exact.
    ///
    /// AC-03.2 is answered elsewhere, not silently: the monotonic-advance and requeue-if-admitted
    /// guarantees are enforced inside Lua against a real Redis, so they are pinned in
    /// <c>Integration/Containers/TaskManagerMultiReplicaTests</c>, and the promise that both scripts are
    /// byte-identical to what shipped before the moments existed is
    /// <c>RedisUpdateStatusScriptFreezeTest</c>. AC-03.5's rendering half — a duration rather than a
    /// timestamp — is the popover's, in <c>TaskManagerIcon.test.tsx</c>.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-03")]
    public partial class Slice03HowLongHasItBeenGoingTest
    {
        // @walking_skeleton @driving_port @real-io @AC-03.1 @AC-03.4 — a real refresh, running, through
        // the production queue: the row says how long it has been going and the number is the server's.
        [Test]
        public async Task A_running_refresh_says_how_long_it_has_been_running()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);
            WhenTheInstanceClockMovesOn(TimeSpan.FromSeconds(12));

            await ThenTheRowForThatTeamSaysItHasBeenGoingFor(team, TimeSpan.FromSeconds(12));
        }

        // @driving_port @real-io @AC-03.1 @AC-03.4 — the waiting half. A queued row's elapsed time is how
        // long it has been waiting, which is the number that separates a queue from a wedge.
        [Test]
        public async Task A_refresh_waiting_its_turn_says_how_long_it_has_been_waiting()
        {
            var running = GivenATeamThatIsRefreshedOnSchedule();
            var waiting = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(running);
            await WhenARefreshOfThatTeamIsAlsoAskedFor(waiting);
            WhenTheInstanceClockMovesOn(TimeSpan.FromMinutes(3));

            await ThenTheRowForThatTeamSaysItHasBeenGoingFor(waiting, TimeSpan.FromMinutes(3));
        }

        // @driving_port @real-io @AC-03.4 — the criterion itself. Between the two readings the test's own
        // wall clock moves by milliseconds and the instance clock by an hour; the answer follows the
        // instance. A duration computed anywhere but here would fail this and pass everything else.
        [Test]
        public async Task Elapsed_time_follows_the_instances_own_clock_and_nothing_else()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            await WhenARefreshOfThatTeamIsUnderWay(team);

            var beforeTheClockMoved = await TheElapsedTimeReportedFor(team);
            WhenTheInstanceClockMovesOn(TimeSpan.FromHours(1));
            var afterTheClockMoved = await TheElapsedTimeReportedFor(team);

            ThenTheAnswerGrewByExactly(beforeTheClockMoved, afterTheClockMoved, TimeSpan.FromHours(1));
        }

        // @driving_port @real-io @AC-03.3 — a coalesced follow-up is new work waiting, not the old work
        // still waiting. Carrying the original wait on would report an hour against something admitted a
        // moment ago, which is the exact misreading the slice exists to prevent.
        [Test]
        public async Task A_coalesced_follow_up_starts_its_wait_again()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();
            GivenWorkForThatTeamWasAdmittedAnHourAgo(team);

            WhenTheSameWorkIsCoalescedIntoAFollowUp(team);

            await ThenTheRowForThatTeamSaysItHasBeenGoingFor(team, TimeSpan.Zero);
        }

        // @driving_port @real-io @error @AC-03.5 — mid-rolling-upgrade, which is a mixed list rather than
        // an empty one: one replica is on the older build and records nothing, the other is on this one and
        // records both moments. The old entry has to survive without a duration rather than invent one or
        // take the list down with it, while the new entry still answers.
        //
        // The control row is what stops this passing for the wrong reason. Asserting only that a duration
        // is absent is satisfied by a build where durations do not exist at all.
        [Test]
        public async Task Work_admitted_before_the_upgrade_is_listed_without_a_duration_beside_work_that_has_one()
        {
            var admittedByTheOldBuild = GivenATeamThatIsRefreshedOnSchedule();
            var admittedByThisOne = GivenATeamThatIsRefreshedOnSchedule();

            GivenWorkForThatTeamWasAdmittedByAReplicaThatRecordedNoMoments(admittedByTheOldBuild);
            GivenWorkForThatTeamWasAdmittedAnHourAgo(admittedByThisOne);

            await ThenTheRowForThatTeamIsListedByNameWithNoDurationAtAll(admittedByTheOldBuild);
            await ThenTheRowForThatTeamSaysItHasBeenGoingFor(admittedByThisOne, TimeSpan.FromHours(1));
        }

        // @driving_port @real-io @error @AC-03.5 — the half-recorded case, which is the one a fallback
        // gets wrong quietly: an entry that has an admission moment but never got a start moment is
        // running, so falling back to the admission would report the wait as the run — forty minutes
        // against something that may have started seconds ago.
        [Test]
        public async Task A_running_refresh_whose_start_went_unrecorded_says_nothing_rather_than_reporting_its_wait()
        {
            var missingItsStart = GivenATeamThatIsRefreshedOnSchedule();
            var recordedInFull = GivenATeamThatIsRefreshedOnSchedule();

            GivenThatWorkIsRunningButOnlyItsAdmissionWasEverRecorded(missingItsStart);
            GivenWorkForThatTeamWasAdmittedAnHourAgo(recordedInFull);

            await ThenTheRowForThatTeamIsListedByNameWithNoDurationAtAll(missingItsStart);
            await ThenTheRowForThatTeamSaysItHasBeenGoingFor(recordedInFull, TimeSpan.FromHours(1));
        }

        // @driving_port @real-io @error @AC-03.5 — work that has finished is neither running nor waiting, so
        // it is not on a list of what is running. It can still be in the store: briefly as it ends, and for
        // good if the replica running it died in that window, because nothing reaps a key its own replica
        // never removed. A row like that used to be listed and counted for the life of the deployment while
        // the endpoint beside it called the same instance idle.
        [Test]
        public async Task Work_that_has_already_finished_is_not_on_the_list_at_all()
        {
            var finished = GivenATeamThatIsRefreshedOnSchedule();
            var stillWaiting = GivenATeamThatIsRefreshedOnSchedule();

            GivenThatWorkFinishedAnHourAgoButIsStillInTheStore(finished);
            GivenWorkForThatTeamWasAdmittedAnHourAgo(stillWaiting);

            await ThenTheTaskListDoesNotMention(finished);
            await ThenTheRowForThatTeamSaysItHasBeenGoingFor(stillWaiting, TimeSpan.FromHours(1));
        }

        // @driving_port @real-io @error @AC-03.4 — the moments are written by whichever replica handled
        // the transition and the elapsed time computed by whichever replica answers the read. Their clocks
        // do not agree to the millisecond, so "started in the future" is an ordinary state, and a row that
        // reports it as a negative duration reads as broken rather than as new.
        [Test]
        public async Task A_replica_whose_clock_runs_behind_never_makes_a_row_say_it_started_in_the_future()
        {
            var team = GivenATeamThatIsRefreshedOnSchedule();

            GivenThatWorkWasStampedByAReplicaWhoseClockIsAheadOfThisOne(team);

            await ThenTheRowForThatTeamSaysItHasBeenGoingFor(team, TimeSpan.Zero);
        }
    }
}
