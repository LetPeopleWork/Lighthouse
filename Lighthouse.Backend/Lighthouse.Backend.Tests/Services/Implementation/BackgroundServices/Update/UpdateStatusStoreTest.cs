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
    }
}
