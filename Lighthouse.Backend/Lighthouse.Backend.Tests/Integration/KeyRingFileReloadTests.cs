using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace Lighthouse.Backend.Tests.Integration
{
    // A cluster does not rewrite a projected secret in place. It writes the new content into a fresh
    // directory and then moves one link, so the path the application was given never changes while
    // everything underneath it does. Nothing that stands in for a filesystem reproduces that, and it is
    // exactly the shape that defeats watching the file for changes - which is why this probe uses a real
    // directory, real links and a real move.
    [Platform(Include = "Linux", Reason = "The projection this reproduces is a directory symlink swapped by rename, which needs a filesystem that has them and a process allowed to make them.")]
    public class KeyRingFileReloadTests
    {
        private const string TheKeyItStartedOn = "k-2026-08-01-01";

        private const string TheKeyTheOperatorAdded = "k-2026-08-17-01";

        private const string CurrentDataLinkName = "..data";

        private const string KeysFileName = "keys";

        private string mountDirectory = string.Empty;

        [SetUp]
        public void SetUp()
        {
            mountDirectory = Path.Combine(
                Path.GetTempPath(), "lighthouse-key-ring-reload", Guid.NewGuid().ToString("n"));

            Directory.CreateDirectory(mountDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(mountDirectory))
            {
                Directory.Delete(mountDirectory, recursive: true);
            }
        }

        [Test]
        public void ReadOnce_TheProjectionReplacedTheWayAClusterReplacesIt_IsStillNoticed()
        {
            Project("..2026_08_01", RingTextFor((TheKeyItStartedOn, MaterialOf(7))));

            var holder = HolderOn(TheKeyItStartedOn);
            var watcher = WatcherOver(holder);

            watcher.ReadOnce();

            Project("..2026_08_17", RingTextFor((TheKeyTheOperatorAdded, MaterialOf(41)), (TheKeyItStartedOn, MaterialOf(7))));

            watcher.ReadOnce();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current.ActiveKey.Id, Is.EqualTo(TheKeyTheOperatorAdded));
                Assert.That(IdsOn(holder.Current), Does.Contain(TheKeyItStartedOn));
            }
        }

        [Test]
        public void ReadOnce_TheFileItselfNeverChanged_IsWhyWatchingItWouldHaveMissedThis()
        {
            Project("..2026_08_01", RingTextFor((TheKeyItStartedOn, MaterialOf(7))));

            var keysPath = Path.Combine(mountDirectory, KeysFileName);
            var whatTheApplicationWasGiven = new FileInfo(keysPath).LinkTarget;

            Project("..2026_08_17", RingTextFor((TheKeyTheOperatorAdded, MaterialOf(41))));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(new FileInfo(keysPath).LinkTarget, Is.EqualTo(whatTheApplicationWasGiven),
                    "The path the application holds changed, so this no longer reproduces a cluster's projection.");
                Assert.That(File.ReadAllText(keysPath), Does.Contain(TheKeyTheOperatorAdded),
                    "Reading the same path did not hand back the new content, so the projection is not wired up.");
            }
        }

        // An operator who drops the old key before asking for the re-encryption is the degenerate case the
        // design accepts rather than prevents: custody is theirs. What it owes them is that the secrets left
        // behind say so, rather than being handed to a work tracking system that rejects them over and over.
        // The outcome already shipped; what is new here is a reload as the way into it.
        [Test]
        public void ReadOnce_AKeyDroppedByAReload_LeavesWhatWasWrittenUnderItReadingAsUnreadable()
        {
            var theKeyThatGoesAway = MaterialOf(7);

            Project("..2026_08_01", RingTextFor((TheKeyItStartedOn, theKeyThatGoesAway)));

            var holder = HolderOn(TheKeyItStartedOn);
            var crypto = new CryptoService(holder, NullLogger<CryptoService>.Instance);

            var storedBefore = SecretEnvelope
                .Protect("a personal access token", TheKeyItStartedOn, theKeyThatGoesAway)
                .Format();

            var watcher = WatcherOver(holder);
            watcher.ReadOnce();

            Assert.That(crypto.Read(storedBefore).State, Is.EqualTo(SecretState.Envelope),
                "The credential could not be read before the key was dropped, so the assertion below would " +
                "prove nothing about dropping it.");

            Project("..2026_08_17", RingTextFor((TheKeyTheOperatorAdded, MaterialOf(41))));

            watcher.ReadOnce();

            var afterwards = crypto.Read(storedBefore);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterwards.State, Is.EqualTo(SecretState.Unreadable));
                Assert.That(afterwards.PlainText, Is.Null);
                Assert.That(afterwards.KeyId, Is.EqualTo(TheKeyItStartedOn),
                    "The credential names the key it was written under, which is the one thing that tells an " +
                    "operator which key to put back.");
            }
        }

        // The layout a kubelet writes: the content in a directory named for when it was written, one link
        // naming which of those directories is current, and the file the application was given pointing
        // through that link. Replacing the content means swapping the middle link by rename, so the swap is
        // atomic and the outer path is never touched.
        private void Project(string versionDirectoryName, string ringText)
        {
            var versionDirectory = Path.Combine(mountDirectory, versionDirectoryName);
            Directory.CreateDirectory(versionDirectory);
            File.WriteAllText(Path.Combine(versionDirectory, KeysFileName), ringText);

            var currentDataLink = Path.Combine(mountDirectory, CurrentDataLinkName);

            // A kubelet swaps this link with a rename, which replaces what is there in one step. Neither move
            // in the framework will do that to a link whose target is a directory, so it is taken down and put
            // back instead. What that costs is the atomicity, and the atomicity is not what this probe is
            // about: what defeats watching the file is that the content moves out from under a link that never
            // itself changes, and taking the link down and putting it back reproduces exactly that.
            if (Directory.Exists(currentDataLink))
            {
                Directory.Delete(currentDataLink);
            }

            Directory.CreateSymbolicLink(currentDataLink, versionDirectoryName);

            var keysPath = Path.Combine(mountDirectory, KeysFileName);

            if (!File.Exists(keysPath))
            {
                File.CreateSymbolicLink(keysPath, Path.Combine(CurrentDataLinkName, KeysFileName));
            }
        }

        private KeyRingFileWatcher WatcherOver(EncryptionKeyRingHolder holder)
        {
            return new KeyRingFileWatcher(
                new MountedFileKeyRingSource(Path.Combine(mountDirectory, KeysFileName), new PhysicalKeyStoreFileSystem()),
                holder,
                TimeProvider.System,
                TimeSpan.FromSeconds(30),
                NullLogger<KeyRingFileWatcher>.Instance);
        }

        private static EncryptionKeyRingHolder HolderOn(string keyId)
        {
            return new EncryptionKeyRingHolder(
                new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, new EncryptionKey(keyId, MaterialOf(7)))
                    .WithLegacyDefault());
        }

        private static List<string> IdsOn(EncryptionKeyRing ring)
        {
            return [.. ring.RetiredKeys.Prepend(ring.ActiveKey).Select(key => key.Id)];
        }

        private static string RingTextFor(params (string Id, byte[] Material)[] keys)
        {
            return string.Join(',', keys.Select(key => $"{key.Id}:{Convert.ToBase64String(key.Material)}"));
        }

        private static byte[] MaterialOf(byte seed)
        {
            var material = new byte[EncryptionKey.MaterialLength];
            Array.Fill(material, seed);

            return material;
        }
    }
}
