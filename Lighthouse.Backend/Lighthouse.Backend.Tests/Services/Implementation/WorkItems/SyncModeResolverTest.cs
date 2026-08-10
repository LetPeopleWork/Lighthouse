using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    /// <summary>
    /// Epic #5687 D8: an update is Full or Delta, never partial, and every ambiguity resolves to Full.
    /// The resolver is a total function of what the refresh already holds, so each branch is asserted
    /// directly rather than through a refresh that happens to exercise it.
    /// </summary>
    public class SyncModeResolverTest
    {
        private static readonly DateTime AWhileAgo = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

        [Test]
        public void Resolve_EverythingStampedAndScanSucceeded_ResolvesToDelta()
        {
            var mode = SyncModeResolver.Resolve(true, Stored(AWhileAgo, AWhileAgo), true, false);

            Assert.That(mode, Is.EqualTo(SyncMode.Delta));
        }

        [Test]
        public void Resolve_ConnectionCannotBeScanned_ResolvesToFull()
        {
            var mode = SyncModeResolver.Resolve(false, Stored(AWhileAgo, AWhileAgo), true, false);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_NothingStoredYet_ResolvesToFull()
        {
            var mode = SyncModeResolver.Resolve(true, Stored(), true, false);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_OneStoredRecordHasNoRemoteChangeStamp_ResolvesToFull()
        {
            var mode = SyncModeResolver.Resolve(true, Stored(AWhileAgo, null), true, false);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_ScanFailed_ResolvesToFull()
        {
            var mode = SyncModeResolver.Resolve(true, Stored(AWhileAgo, AWhileAgo), false, false);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_FetchShapeChanged_ResolvesToFull()
        {
            var mode = SyncModeResolver.Resolve(true, Stored(AWhileAgo, AWhileAgo), true, true);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        private static List<WorkItem> Stored(params DateTime?[] remoteChangeStamps)
            => [.. remoteChangeStamps.Select(stamp => new WorkItem { LastChangedRemote = stamp })];
    }
}
