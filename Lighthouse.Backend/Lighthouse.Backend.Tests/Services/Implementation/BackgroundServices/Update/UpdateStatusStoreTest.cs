using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    [TestFixture]
    public class UpdateStatusStoreTest
    {
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
    }
}
