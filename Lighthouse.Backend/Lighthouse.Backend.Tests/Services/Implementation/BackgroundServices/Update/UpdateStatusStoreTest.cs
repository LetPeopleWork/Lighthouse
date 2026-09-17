using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    [TestFixture]
    public class UpdateStatusStoreTest
    {
        private static readonly UpdateKey KeyWaitingToStart = new(UpdateType.Team, 1);

        private static readonly UpdateKey KeyAlreadyRunning = new(UpdateType.Team, 2);

        private static readonly UpdateKey KeyNobodyAdmitted = new(UpdateType.Team, 3);

        private static readonly UpdateKey[] NoKeys = Array.Empty<UpdateKey>();

        private static readonly UpdateKey[] OnlyAKeyNobodyAdmitted = [KeyNobodyAdmitted];

        private static readonly UpdateKey[] OnlyTheKeyAlreadyRunning = [KeyAlreadyRunning];

        private static readonly UpdateKey[] TheQueuedKeyAmongUnrelatedOnes = [KeyNobodyAdmitted, KeyWaitingToStart];

        [Test]
        public void Advance_RegressingProgress_NeverObservesRegressedProgress()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 7);
            store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = 7, Status = UpdateProgress.Queued });
            store.Advance(key, UpdateProgress.InProgress);

            store.Advance(key, UpdateProgress.Queued);

            store.TryGet(key, out var observed);
            Assert.That(observed!.Status, Is.EqualTo(UpdateProgress.InProgress),
                "Advance is a monotonic compare-and-set on the UpdateProgress ordinal: a regression to a lower " +
                "ordinal must be rejected, so a reader still observes InProgress (INV-1, not blind last-writer-wins).");
        }

        [Test]
        public void Advance_ForwardProgress_MovesToTheHigherOrdinal()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Features, 3);
            store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Features, Id = 3, Status = UpdateProgress.Queued });

            var result = store.Advance(key, UpdateProgress.Completed);

            Assert.That(result!.Status, Is.EqualTo(UpdateProgress.Completed),
                "a forward advance to a higher ordinal is applied and the post-state is returned");
        }

        [Test]
        public void Requeue_AdmittedKeyPastItsTerminalState_ReturnsItToQueuedAndKeepsItActive()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 5);
            store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = 5, Status = UpdateProgress.Queued });
            store.Advance(key, UpdateProgress.Completed);

            store.Requeue(key);

            store.TryGet(key, out var observed);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(observed!.Status, Is.EqualTo(UpdateProgress.Queued),
                    "Requeue is the deliberate escape from Advance's monotonic guard: a coalesced follow-up needs the key back at Queued.");
                Assert.That(store.HasActiveWork(), Is.True,
                    "A re-queued key must count as active work, so a caller polling for idle waits for the follow-up instead of reading the state it is about to change.");
            }
        }

        [Test]
        public void Requeue_KeyThatWasNeverAdmitted_DoesNotFabricateActiveWork()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);

            store.Requeue(new UpdateKey(UpdateType.Team, 6));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(store.TryGet(new UpdateKey(UpdateType.Team, 6), out _), Is.False);
                Assert.That(store.HasActiveWork(), Is.False,
                    "Requeue must never resurrect an absent key: that would leave a phantom active entry nothing will ever complete, permanently blocking re-admission.");
            }
        }

        [TestCaseSource(nameof(QueuedLookupCases))]
        public void HasQueuedWork_ForTheKeysACallerIsWaitingOn_ReportsOnlyThoseStandingQueued(UpdateKey[] keys, bool expected, string because)
        {
            var store = StoreWithOneKeyQueuedAndOneRunning();

            Assert.That(store.HasQueuedWork(keys), Is.EqualTo(expected), because);
        }

        private static TestCaseData[] QueuedLookupCases()
        {
            return
            [
                new TestCaseData(NoKeys, false, "A caller waiting on nothing is never held back, and an empty ask must not degenerate into a store-wide scan.")
                    .SetName("HasQueuedWork_EmptyKeySet_ReportsNoQueuedWork"),
                new TestCaseData(OnlyAKeyNobodyAdmitted, false, "A key the store never admitted is not queued work, even though an unrelated key in the same store is queued.")
                    .SetName("HasQueuedWork_KeyNotInTheStore_ReportsNoQueuedWork"),
                new TestCaseData(OnlyTheKeyAlreadyRunning, false, "Work that has already started is not waiting to start: a caller whose own key is running would otherwise wait for itself forever.")
                    .SetName("HasQueuedWork_KeyAlreadyRunning_ReportsNoQueuedWork"),
                new TestCaseData(TheQueuedKeyAmongUnrelatedOnes, true, "One key still waiting to start is enough for the caller to hold off, whatever the other keys are doing.")
                    .SetName("HasQueuedWork_OneOfTheKeysStillWaitingToStart_ReportsQueuedWork"),
            ];
        }

        /// <summary>
        /// Only the transition into running records a start. Work that goes straight from waiting to finished
        /// - a refresh that failed before the tracker was reached, or one the queue abandoned - never ran, and
        /// a start moment on it would have the task list report it as having been running all along.
        /// </summary>
        [Test]
        public void Advance_StraightFromWaitingToFinished_RecordsNoStartMoment()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 9);
            store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = 9, Status = UpdateProgress.Queued });

            var finished = store.Advance(key, UpdateProgress.Completed);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(finished?.Status, Is.EqualTo(UpdateProgress.Completed));
                Assert.That(finished?.StartedAt, Is.Null,
                    "It never started, and saying otherwise puts a duration on a row that was only ever waiting.");
            }
        }

        [Test]
        public void Advance_IntoRunning_RecordsWhenItStarted()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 10);
            store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = 10, Status = UpdateProgress.Queued });

            var running = store.Advance(key, UpdateProgress.InProgress);

            Assert.That(running?.StartedAt, Is.Not.Null,
                "Without it the row can only say how long it waited, which is the wrong number once it is running.");
        }

        [Test]
        public void CancelIfStillWaiting_WorkThatIsStillWaiting_MarksItCancelledWithoutEverStartingIt()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 11);
            store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = 11, Status = UpdateProgress.Queued });

            var cancelled = store.CancelIfStillWaiting(key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancelled?.Status, Is.EqualTo(UpdateProgress.Cancelled));
                Assert.That(cancelled?.StartedAt, Is.Null,
                    "Nothing ran, so a start moment here would put a duration on work that only ever waited.");
            }
        }

        /// <summary>
        /// The whole reason this is one step rather than a read and then a write. A refresh that is already
        /// talking to a tracker has to keep its row until it stops: marked cancelled, it leaves HasActiveWork
        /// while it is still writing, and whatever was waiting for the instance to go idle stops waiting.
        /// </summary>
        [Test]
        public void CancelIfStillWaiting_WorkTheQueueHasAlreadyStarted_LeavesItRunning()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 12);
            store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = 12, Status = UpdateProgress.Queued });
            store.Advance(key, UpdateProgress.InProgress);

            var cancelled = store.CancelIfStillWaiting(key);

            store.TryGet(key, out var observed);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(cancelled, Is.Null, "There was nothing waiting to cancel, so there is nothing to tell anybody about.");
                Assert.That(observed!.Status, Is.EqualTo(UpdateProgress.InProgress));
                Assert.That(store.HasActiveWork(), Is.True,
                    "It is still running, and anything polling for idle has to keep waiting for it.");
            }
        }

        [Test]
        public void Advance_KeyNobodyAdmitted_AnswersNothingAndAdmitsNothing()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            var key = new UpdateKey(UpdateType.Team, 14);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(store.Advance(key, UpdateProgress.InProgress), Is.Null);
                Assert.That(store.HasActiveWork(), Is.False,
                    "An advance on a key another replica already removed must not bring it back as active work nothing will ever finish.");
            }
        }

        [Test]
        public void CancelIfStillWaiting_KeyNobodyAdmitted_AnswersNothing()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);

            Assert.That(store.CancelIfStillWaiting(new UpdateKey(UpdateType.Team, 13)), Is.Null);
        }

        /// <summary>
        /// Starting and cancelling arrive on different threads and are both about to happen. One of them has
        /// to lose, and which one is not the point - the point is that the pair can never leave a key that is
        /// cancelled and started at once, which is the state nothing downstream knows how to read.
        /// </summary>
        [Test]
        public void CancelIfStillWaiting_RacingTheQueueStartingTheSameWork_NeverLeavesItBothCancelledAndStarted()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);

            for (var attempt = 0; attempt < 200; attempt++)
            {
                var key = new UpdateKey(UpdateType.Team, attempt);
                store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = attempt, Status = UpdateProgress.Queued });

                var starting = Task.Run(() => store.Advance(key, UpdateProgress.InProgress));
                var cancelling = Task.Run(() => store.CancelIfStillWaiting(key));
                Task.WaitAll(starting, cancelling);

                store.TryGet(key, out var observed);
                Assert.That(observed!.Status == UpdateProgress.Cancelled && observed.StartedAt is not null, Is.False,
                    $"Attempt {attempt} left the work cancelled and started at the same time.");
            }
        }

        private static InProcessUpdateStatusStore StoreWithOneKeyQueuedAndOneRunning()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>(), Clocks.SystemUtc);
            store.TryAdmit(KeyWaitingToStart, new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued });
            store.TryAdmit(KeyAlreadyRunning, new UpdateStatus { UpdateType = UpdateType.Team, Id = 2, Status = UpdateProgress.Queued });
            store.Advance(KeyAlreadyRunning, UpdateProgress.InProgress);

            return store;
        }
    }
}
