using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

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
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>());
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
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>());
            var key = new UpdateKey(UpdateType.Features, 3);
            store.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Features, Id = 3, Status = UpdateProgress.Queued });

            var result = store.Advance(key, UpdateProgress.Completed);

            Assert.That(result!.Status, Is.EqualTo(UpdateProgress.Completed),
                "a forward advance to a higher ordinal is applied and the post-state is returned");
        }

        [Test]
        public void Requeue_AdmittedKeyPastItsTerminalState_ReturnsItToQueuedAndKeepsItActive()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>());
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
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>());

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

        private static InProcessUpdateStatusStore StoreWithOneKeyQueuedAndOneRunning()
        {
            var store = new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>());
            store.TryAdmit(KeyWaitingToStart, new UpdateStatus { UpdateType = UpdateType.Team, Id = 1, Status = UpdateProgress.Queued });
            store.TryAdmit(KeyAlreadyRunning, new UpdateStatus { UpdateType = UpdateType.Team, Id = 2, Status = UpdateProgress.Queued });
            store.Advance(KeyAlreadyRunning, UpdateProgress.InProgress);

            return store;
        }
    }
}
