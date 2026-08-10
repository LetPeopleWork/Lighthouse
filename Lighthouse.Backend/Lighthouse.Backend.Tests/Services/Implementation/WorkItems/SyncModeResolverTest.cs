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
            var mode = Resolve();

            Assert.That(mode, Is.EqualTo(SyncMode.Delta));
        }

        [Test]
        public void Resolve_NobodyOptedIn_ResolvesToFull()
        {
            var mode = Resolve(operatorAskedForTheCheaperRefresh: false);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_ConnectionCannotBeScanned_ResolvesToFull()
        {
            var mode = Resolve(trackerCanBeScanned: false);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_NothingStoredYet_ResolvesToFull()
        {
            var mode = Resolve(storedWorkItems: Stored());

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_OneStoredRecordHasNoRemoteChangeStamp_ResolvesToFull()
        {
            var mode = Resolve(storedWorkItems: Stored(AWhileAgo, null));

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_ScanFailed_ResolvesToFull()
        {
            var mode = Resolve(scanSucceeded: false);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        [Test]
        public void Resolve_FetchShapeChanged_ResolvesToFull()
        {
            var mode = Resolve(fetchShapeChanged: true);

            Assert.That(mode, Is.EqualTo(SyncMode.Full));
        }

        /// <summary>
        /// Defaults are the one combination that resolves to Delta, so each test names only the single
        /// input it is about - five bools at a call site say nothing about which one the test is asserting.
        /// </summary>
        private static SyncMode Resolve(
            bool operatorAskedForTheCheaperRefresh = true,
            bool trackerCanBeScanned = true,
            List<WorkItem>? storedWorkItems = null,
            bool scanSucceeded = true,
            bool fetchShapeChanged = false)
            => SyncModeResolver.Resolve(
                operatorAskedForTheCheaperRefresh,
                trackerCanBeScanned,
                storedWorkItems ?? Stored(AWhileAgo, AWhileAgo),
                scanSucceeded,
                fetchShapeChanged);

        private static List<WorkItem> Stored(params DateTime?[] remoteChangeStamps)
            => [.. remoteChangeStamps.Select(stamp => new WorkItem { LastChangedRemote = stamp })];
    }
}
