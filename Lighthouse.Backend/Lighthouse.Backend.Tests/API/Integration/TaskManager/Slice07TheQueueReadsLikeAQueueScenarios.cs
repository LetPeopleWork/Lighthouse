using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic #5511 — Task Manager), slice 07 / ADO #6011: the queue reads
    /// like a queue. Driving port: the System-Administrator-guarded task list on <c>UpdateController</c>,
    /// exercised over HTTP — the same one slices 02 and 03 established. US-07A, AC-07A.1 … AC-07A.3.
    ///
    /// What the maintainer saw on the shipped build was "the first item say queued, the 2nd being
    /// actively refreshed, the third being queued". Nothing was wrong with the rows; the sequence they
    /// arrived in was the store's iteration order, which is hash order under Redis and bucket order in
    /// process. Neither is an order, and a surface about a queue that presents rows in a sequence makes a
    /// claim about them whether it means to or not.
    ///
    /// Two things shape every scenario below. The moments are pinned, because a sort key taken from the
    /// wall clock would put the rows in an order that happens to be right. And the work is put into the
    /// store in an order deliberately unlike the answer, because a scenario that only sometimes catches
    /// an unordered list teaches nobody anything.
    ///
    /// AC-07A.4 … AC-07A.8 are the popover's, in <c>TaskManagerIcon.test.tsx</c>: a spinner rather than a
    /// second-count is a rendering promise and has no backend surface.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5511-task-manager")]
    [Category("slice-07")]
    public partial class Slice07TheQueueReadsLikeAQueueTest
    {
        // @walking_skeleton @driving_port @real-io @AC-07A.1 — a real refresh, running, through the
        // production queue, read back beside work that is genuinely waiting on it.
        [Test]
        [Ignore("pending — DELIVER unskips this first")]
        public async Task The_refresh_that_is_under_way_is_read_first_and_what_is_waiting_follows_it_oldest_first()
        {
            var running = GivenATeamThatIsRefreshedOnSchedule();
            var waitingLongest = GivenATeamThatIsRefreshedOnSchedule();
            var waitingSince = GivenATeamThatIsRefreshedOnSchedule();
            GivenTheTrackerDoesNotAnswerUntilWeSaySo();

            GivenWorkForThatTeamWasAdmitted(waitingLongest, TimeSpan.FromMinutes(8));
            GivenWorkForThatTeamWasAdmitted(waitingSince, TimeSpan.FromMinutes(5));
            await WhenARefreshOfThatTeamIsUnderWay(running);

            // The running row is the newest moment in this list, because a refresh starts after everything
            // that was waiting for it was admitted. So a list sorted on "whichever moment the row carries"
            // puts the one thing actually happening at the bottom - which is the shape the maintainer read
            // as the surface being wrong about what it was doing.
            await ThenTheListReads(running, waitingLongest, waitingSince);
        }

        // @driving_port @AC-07A.1 — the waiting half on its own, admitted newest-first so that a list
        // handed back in the order it was filled is wrong rather than coincidentally right.
        [Test]
        [Ignore("pending")]
        public async Task Work_that_has_been_waiting_longest_is_the_next_thing_the_queue_will_reach()
        {
            var oldestFirst = GivenAQueueAdmittedInTheReverseOfTheOrderItShouldRead(3);

            await ThenTheListReads(oldestFirst);
        }

        // @driving_port @error @AC-07A.2 — the promise the defect is actually about. One read of an
        // unordered list can come out right; five agreeing on a twelve-row answer cannot.
        [Test]
        [Ignore("pending")]
        public async Task The_queue_reads_the_same_way_every_time_it_is_read()
        {
            var oldestFirst = GivenAQueueAdmittedInTheReverseOfTheOrderItShouldRead(ALongQueue);

            await ThenTheListReadsTheSameWayEveryTime(oldestFirst);
        }

        // @driving_port @error @AC-07A.2 — the case the moments cannot settle. Two admissions inside one
        // tick of the clock share a moment, and a comparison that stops there hands the order back to the
        // store.
        //
        // Not held back, because it passes on arrival and will keep passing while the instance keeps its
        // admitted work in a dictionary: a dictionary hands back the same sequence every time as long as
        // nothing is added or removed, so re-shuffling is not something this substrate does. Where it does
        // happen is a Redis hash, and a scenario that needs a container belongs beside the other ones that
        // do. This is therefore a guard rather than a driver, and worth having as one: it is what turns a
        // sort that leaves equal rows to chance into a failure rather than an oddity somebody notices in
        // production.
        [Test]
        public async Task Work_admitted_in_the_very_same_instant_still_settles_into_one_order()
        {
            var admittedTogether = GivenSeveralTeamsAdmittedInTheSameInstant(4);

            await ThenTheListAlwaysReadsTheSameWayWhateverOrderThatIs(admittedTogether);
        }

        // @driving_port @error @AC-07A.3 — mid-rolling-upgrade a replica on the older build admits work
        // and records nothing. The row has no place to claim in the order, so it takes the last one, and
        // it keeps everything else about itself.
        [Test]
        [Ignore("pending")]
        public async Task Work_whose_admission_was_never_recorded_waits_at_the_end_and_still_says_it_is_waiting()
        {
            var recorded = GivenATeamThatIsRefreshedOnSchedule();
            var unrecorded = GivenATeamThatIsRefreshedOnSchedule();

            // Seeded before the one that has a moment, so a list that keeps the order it was filled in
            // fails here rather than passing on the arrangement.
            GivenWorkForThatTeamWasAdmittedByAReplicaThatRecordedNoMoments(unrecorded);
            GivenWorkForThatTeamWasAdmitted(recorded, TimeSpan.FromMinutes(2));

            await ThenTheListReads(recorded, unrecorded);
            await ThenTheRowForThatTeamStillSaysItIs(unrecorded, UpdateProgress.Queued);
        }

        // @driving_port @error @AC-07A.3 @AC-07A.1 — the same missing moment, on the row that is running.
        // Sorting it to the very end would drop the one thing the instance is doing below three things it
        // is not, which is the reading this slice exists to fix. Last of what is running, not last of all.
        [Test]
        [Ignore("pending")]
        public async Task A_refresh_that_is_running_is_read_first_even_though_nobody_recorded_when_it_started()
        {
            var runningWithNoStartRecorded = GivenATeamThatIsRefreshedOnSchedule();
            var waitingLongest = GivenATeamThatIsRefreshedOnSchedule();
            var waitingSince = GivenATeamThatIsRefreshedOnSchedule();

            GivenWorkForThatTeamWasAdmitted(waitingLongest, TimeSpan.FromMinutes(9));
            GivenWorkForThatTeamWasAdmitted(waitingSince, TimeSpan.FromMinutes(4));
            GivenWorkForThatTeamIsRunningButItsStartWentUnrecorded(runningWithNoStartRecorded);

            await ThenTheListReads(runningWithNoStartRecorded, waitingLongest, waitingSince);
            await ThenTheRowForThatTeamStillSaysItIs(runningWithNoStartRecorded, UpdateProgress.InProgress);
        }
    }
}
