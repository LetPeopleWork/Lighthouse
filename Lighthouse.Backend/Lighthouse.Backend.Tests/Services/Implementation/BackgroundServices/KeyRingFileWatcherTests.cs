using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    public class KeyRingFileWatcherTests
    {
        private const string MountedPath = "/etc/lighthouse/encryption/keys";

        private const string TheKeyItStartedOn = "k-2026-08-01-01";

        private const string TheKeyTheOperatorAdded = "k-2026-08-17-01";

        [Test]
        public void ReadOnce_AKeyAddedAheadOfTheOldOne_BecomesTheKeyInForce()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);
            var added = MaterialOf(41);

            files.Place(MountedPath, RingTextFor((TheKeyTheOperatorAdded, added), (TheKeyItStartedOn, MaterialOf(7))));

            WatcherOver(files, holder, out _).ReadOnce();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current.ActiveKey.Id, Is.EqualTo(TheKeyTheOperatorAdded));
                Assert.That(holder.Current.ActiveKey.Material.ToArray(), Is.EqualTo(added));
                Assert.That(IdsOn(holder.Current), Does.Contain(TheKeyItStartedOn));
            }
        }

        [Test]
        public void ReadOnce_AKeyPickedUp_SaysSoNamingWhatItNowHolds()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);

            files.Place(MountedPath, RingTextFor((TheKeyTheOperatorAdded, MaterialOf(41)), (TheKeyItStartedOn, MaterialOf(7))));

            var watcher = WatcherOver(files, holder, out var logger);
            watcher.ReadOnce();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Logged(logger, LogLevel.Information), Has.Some.Contains("encryption.keyring.reloaded"));
                Assert.That(Logged(logger, LogLevel.Information), Has.Some.Contains(TheKeyTheOperatorAdded));
            }
        }

        // The published key is appended to every ring this instance resolves so that an upgraded install can
        // still read what it stored before the epic. A reload that dropped it would make those secrets
        // unreadable at the moment an operator was doing something entirely unrelated.
        [Test]
        public void ReadOnce_AKeyPickedUp_KeepsThePublishedKeyBehindItForReading()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);

            files.Place(MountedPath, RingTextFor((TheKeyTheOperatorAdded, MaterialOf(41))));

            WatcherOver(files, holder, out _).ReadOnce();

            Assert.That(IdsOn(holder.Current), Does.Contain(LegacyDefaultEncryptionKey.Id));
        }

        [Test]
        public void ReadOnce_ContentThatIsNotAKeyRing_LeavesTheKeysInForceExactlyWhereTheyAre()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);
            var before = holder.Current;

            files.Place(MountedPath, "this is not a key ring at all");

            var watcher = WatcherOver(files, holder, out var logger);
            watcher.ReadOnce();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current, Is.EqualTo(before));
                Assert.That(Logged(logger, LogLevel.Error), Is.Not.Empty);
                Assert.That(Logged(logger, LogLevel.Information), Is.Empty);
            }
        }

        [Test]
        public void ReadOnce_AFileHoldingNothingAtAll_IsRefusedTheSameWay()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);
            var before = holder.Current;

            files.Place(MountedPath, "   ");

            var watcher = WatcherOver(files, holder, out var logger);
            watcher.ReadOnce();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current, Is.EqualTo(before));
                Assert.That(Logged(logger, LogLevel.Error), Is.Not.Empty);
            }
        }

        [Test]
        public void ReadOnce_ThePublishedKeyArrivingAsTheKeyToWriteUnder_IsRefused()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);
            var before = holder.Current;

            files.Place(MountedPath, $"k-mine-01:{PublishedKeyMaterial()}");

            var watcher = WatcherOver(files, holder, out var logger);
            watcher.ReadOnce();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current, Is.EqualTo(before));
                Assert.That(Logged(logger, LogLevel.Error), Has.Some.Contains("published with Lighthouse"));
            }
        }

        // Custody belongs to the operator, so removing a key is theirs to do and this does not argue with it.
        // What it does do is say which key went away, because the secrets still written under it stop being
        // readable at that moment and nothing else will point at the reason.
        [Test]
        public void ReadOnce_AKeyTheOperatorRemoved_IsAppliedAndWarnedAbout()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);

            files.Place(MountedPath, RingTextFor((TheKeyTheOperatorAdded, MaterialOf(41))));

            var watcher = WatcherOver(files, holder, out var logger);
            watcher.ReadOnce();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(IdsOn(holder.Current), Does.Not.Contain(TheKeyItStartedOn));
                Assert.That(Logged(logger, LogLevel.Warning), Has.Some.Contains(TheKeyItStartedOn));
                Assert.That(Logged(logger, LogLevel.Information), Is.Empty);
            }
        }

        [Test]
        public void ReadOnce_TheSameContentReadManyTimes_ReplacesNothingAndSaysNothing()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);

            files.Place(MountedPath, RingTextFor((TheKeyTheOperatorAdded, MaterialOf(41))));

            var watcher = WatcherOver(files, holder, out var logger);
            watcher.ReadOnce();

            var afterTheChange = holder.Current;
            logger.Invocations.Clear();

            for (var tick = 0; tick < 25; tick++)
            {
                watcher.ReadOnce();
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current, Is.EqualTo(afterTheChange));
                Assert.That(logger.Invocations, Is.Empty);
            }
        }

        // The ring the instance booted on came out of this same file, so the first read after a start is of
        // content that changed nothing. Announcing a reload there would tell an operator a rotation landed on
        // an instance that had merely been restarted.
        [Test]
        public void ReadOnce_ContentThatMatchesTheRingItBootedOn_SaysNothing()
        {
            var files = new MountedKeysFile();
            var material = MaterialOf(7);
            var holder = new EncryptionKeyRingHolder(
                new EncryptionKeyRing(KeyCustody.SuppliedByExternalSecret, new EncryptionKey(TheKeyItStartedOn, material))
                    .WithLegacyDefault());

            files.Place(MountedPath, RingTextFor((TheKeyItStartedOn, material)));

            var watcher = WatcherOver(files, holder, out var logger);
            watcher.ReadOnce();

            Assert.That(logger.Invocations, Is.Empty);
        }

        [Test]
        public void ReadOnce_AFileThatWentAway_LeavesTheKeysInForceAndIsReportedOnce()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);
            var before = holder.Current;

            var watcher = WatcherOver(files, holder, out var logger);
            watcher.ReadOnce();
            watcher.ReadOnce();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(holder.Current, Is.EqualTo(before));
                Assert.That(TimesLogged(logger, LogLevel.Error), Is.EqualTo(1));
                Assert.That(Logged(logger, LogLevel.Error), Has.Some.Contains(MountedPath));
            }
        }

        [Test]
        public void ReadOnce_AFileThatComesBackAfterAFailure_IsJudgedAfresh()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);

            var watcher = WatcherOver(files, holder, out _);
            watcher.ReadOnce();

            files.Place(MountedPath, RingTextFor((TheKeyTheOperatorAdded, MaterialOf(41))));
            watcher.ReadOnce();

            Assert.That(holder.Current.ActiveKey.Id, Is.EqualTo(TheKeyTheOperatorAdded));
        }

        [Test]
        public void ReadOnce_ContentThatWillNotParse_IsNotComplainedAboutOverAndOver()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);

            files.Place(MountedPath, "still not a key ring");

            var watcher = WatcherOver(files, holder, out var logger);

            for (var tick = 0; tick < 10; tick++)
            {
                watcher.ReadOnce();
            }

            Assert.That(TimesLogged(logger, LogLevel.Error), Is.EqualTo(1));
        }

        [Test]
        public void ReadOnce_NothingItWrites_CarriesKeyMaterialInAnyEncoding()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);
            var added = MaterialOf(41);

            var watcher = WatcherOver(files, holder, out var logger);

            files.Place(MountedPath, RingTextFor((TheKeyTheOperatorAdded, added)));
            watcher.ReadOnce();

            files.Place(MountedPath, "not a key ring");
            watcher.ReadOnce();

            var written = EverythingHandedToTheLoggingPipeline(logger);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(written, Is.Not.Empty, "Nothing was written, so this would pass whatever the log carried.");
                Assert.That(
                    written.Where(line => HoldsMaterial(line, added, MaterialOf(7))).ToList(), Is.Empty,
                    "Key material reached the logging pipeline. A structured property is how it gets there " +
                    "unnoticed: the sentence looks harmless while the property beside it carries the key " +
                    "into every sink the log is shipped to.");
            }
        }

        [Test]
        public async Task ExecuteAsync_ReadsTheFileOnEveryTick()
        {
            var files = new MountedKeysFile();
            var holder = HolderOn(TheKeyItStartedOn);
            var time = new FakeTimeProvider();

            files.Place(MountedPath, RingTextFor((TheKeyItStartedOn, MaterialOf(7))));

            var watcher = WatcherOver(files, holder, out _, time, TimeSpan.FromSeconds(30));

            await watcher.StartAsync(CancellationToken.None);

            try
            {
                Assert.That(files.TimesRead, Is.Zero, "A read happened before any interval had elapsed.");

                await AdvanceUntil(time, () => files.TimesRead >= 1);
                await AdvanceUntil(time, () => files.TimesRead >= 2);
            }
            finally
            {
                await watcher.StopAsync(CancellationToken.None);
            }
        }

        [Test]
        public void IntervalFrom_NothingConfigured_IsThirtySeconds()
        {
            Assert.That(KeyRingFileWatcher.IntervalFrom(null), Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        [TestCase(0)]
        [TestCase(-5)]
        public void IntervalFrom_AnIntervalThatIsNoInterval_IsThirtySeconds(int configured)
        {
            Assert.That(KeyRingFileWatcher.IntervalFrom(configured), Is.EqualTo(TimeSpan.FromSeconds(30)));
        }

        [Test]
        public void IntervalFrom_AnOperatorWhoWantsToWaitLonger_IsTakenAtTheirWord()
        {
            Assert.That(KeyRingFileWatcher.IntervalFrom(120), Is.EqualTo(TimeSpan.FromMinutes(2)));
        }

        // The interval is advanced until the read lands rather than exactly once, because a hosted service is
        // started asynchronously and its timer may not exist yet at the moment the first advance happens - an
        // advance nothing is waiting on is simply lost, and the test would then wait for a tick that can no
        // longer come.
        private static async Task AdvanceUntil(FakeTimeProvider time, Func<bool> condition)
        {
            var giveUpAt = DateTime.UtcNow.AddSeconds(10);

            while (!condition() && DateTime.UtcNow < giveUpAt)
            {
                time.Advance(TimeSpan.FromSeconds(30));
                await Task.Delay(10);
            }

            Assert.That(condition(), Is.True, "The watcher never got round to reading the file.");
        }

        private static KeyRingFileWatcher WatcherOver(
            MountedKeysFile files,
            EncryptionKeyRingHolder holder,
            out Mock<ILogger<KeyRingFileWatcher>> logger,
            TimeProvider? time = null,
            TimeSpan? interval = null)
        {
            logger = new Mock<ILogger<KeyRingFileWatcher>>();
            logger.Setup(written => written.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            return new KeyRingFileWatcher(
                new MountedFileKeyRingSource(MountedPath, files),
                holder,
                time ?? TimeProvider.System,
                interval ?? TimeSpan.FromSeconds(30),
                logger.Object);
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

        // One call to a logger renders as a sentence plus one entry per structured property, so counting what
        // was written is not the same question as counting how often it was written.
        private static int TimesLogged(Mock<ILogger<KeyRingFileWatcher>> logger, LogLevel level)
        {
            return logger.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
                .Count(invocation => Equals(invocation.Arguments[0], level));
        }

        private static List<string> Logged(Mock<ILogger<KeyRingFileWatcher>> logger, LogLevel level)
        {
            return [.. logger.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
                .Where(invocation => Equals(invocation.Arguments[0], level))
                .SelectMany(invocation => Rendered(invocation.Arguments[2]))];
        }

        private static List<string> EverythingHandedToTheLoggingPipeline(Mock<ILogger<KeyRingFileWatcher>> logger)
        {
            return [.. logger.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
                .SelectMany(invocation => Rendered(invocation.Arguments[2]))];
        }

        // The rendered sentence and each structured property separately: material in a property would never
        // show up in a check that only read the sentence.
        private static List<string> Rendered(object? state)
        {
            var written = new List<string> { state?.ToString() ?? string.Empty };

            if (state is IReadOnlyList<KeyValuePair<string, object?>> properties)
            {
                written.AddRange(properties.Select(property => $"{property.Key}={property.Value}"));
            }

            return written;
        }

        private static bool HoldsMaterial(string written, params byte[][] material)
        {
            return Array.Exists(material, bytes =>
                written.Contains(Convert.ToBase64String(bytes), StringComparison.OrdinalIgnoreCase) ||
                written.Contains(Convert.ToHexString(bytes), StringComparison.OrdinalIgnoreCase));
        }

        private static string PublishedKeyMaterial()
        {
            var probe = new byte[EncryptionKey.MaterialLength];

            return Convert.ToBase64String(
                new EncryptionKeyRing(new EncryptionKey("k-not-in-use", probe))
                    .WithLegacyDefault()
                    .RetiredKeys[0]
                    .Material
                    .ToArray());
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

        // The file a cluster projects, without a cluster. Only the two operations a reload performs are
        // real here; the rest of the contract exists so this can stand in for the mounted secret.
        private sealed class MountedKeysFile : IKeyStoreFileSystem
        {
            private readonly Dictionary<string, byte[]> files = new(StringComparer.Ordinal);

            public int TimesRead { get; private set; }

            public void Place(string path, string contents)
            {
                files[path] = Encoding.UTF8.GetBytes(contents);
            }

            public bool FileExists(string path)
            {
                return files.ContainsKey(path);
            }

            public byte[] ReadAllBytes(string path)
            {
                TimesRead++;

                return files[path];
            }

            public void WriteAllBytes(string path, byte[] contents)
            {
                throw new NotSupportedException("A mounted key file is never written by Lighthouse.");
            }

            public void Move(string sourcePath, string destinationPath)
            {
                throw new NotSupportedException("A mounted key file is never written by Lighthouse.");
            }
        }
    }
}
