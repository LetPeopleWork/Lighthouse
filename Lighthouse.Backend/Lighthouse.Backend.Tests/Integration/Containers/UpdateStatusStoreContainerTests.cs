using System.Collections.Concurrent;
using System.Globalization;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
using StackExchange.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class UpdateStatusStoreContainerTests
    {
        private static readonly UpdateKey KeyWaitingToStart = new(UpdateType.Team, 21);

        private static readonly UpdateKey KeyAlreadyRunning = new(UpdateType.Team, 22);

        private static readonly UpdateKey KeyNobodyAdmitted = new(UpdateType.Team, 23);

        internal static readonly (Type Store, Func<IConnectionMultiplexer, IUpdateStatusStore> Build)[] StoresComparedAgainstEachOther =
        [
            (typeof(InProcessUpdateStatusStore), _ => new InProcessUpdateStatusStore(new ConcurrentDictionary<UpdateKey, UpdateStatus>())),
            (typeof(RedisUpdateStatusStore), multiplexer => new RedisUpdateStatusStore(multiplexer)),
        ];

        private static readonly (string Description, UpdateKey[] Keys)[] QueuedLookups =
        [
            ("a caller waiting on nothing", []),
            ("only a key nobody admitted", [KeyNobodyAdmitted]),
            ("only a key that has already started running", [KeyAlreadyRunning]),
            ("a key still waiting to start, among unrelated ones", [KeyNobodyAdmitted, KeyWaitingToStart]),
        ];

        [Test]
        public async Task SharedAdvance_ConcurrentWriters_OrdinalNeverRegresses()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var key = new UpdateKey(UpdateType.Team, 11);
            var podA = new RedisUpdateStatusStore(multiplexer);
            var podB = new RedisUpdateStatusStore(multiplexer);
            podA.TryAdmit(key, new UpdateStatus { UpdateType = UpdateType.Team, Id = 11, Status = UpdateProgress.Queued });

            var lifecycle = new[]
            {
                UpdateProgress.InProgress,
                UpdateProgress.Completed,
                UpdateProgress.Queued,
                UpdateProgress.InProgress,
            };

            var observedRegressions = new ConcurrentBag<string>();

            var writers = Enumerable.Range(0, 8).Select(writer => Task.Run(() =>
            {
                var store = writer % 2 == 0 ? podA : podB;
                var highestObserved = UpdateProgress.Queued;

                foreach (var step in Enumerable.Repeat(lifecycle, 6).SelectMany(steps => steps))
                {
                    store.Advance(key, step);
                    store.TryGet(key, out var observed);

                    if ((int)observed!.Status < (int)highestObserved)
                    {
                        observedRegressions.Add($"observed {observed.Status} after having observed {highestObserved}");
                    }

                    highestObserved = (UpdateProgress)Math.Max((int)highestObserved, (int)observed.Status);
                }
            })).ToArray();

            await Task.WhenAll(writers);

            podA.TryGet(key, out var finalStatus);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(observedRegressions, Is.Empty,
                    "no reader ever observes a regressed UpdateProgress ordinal under concurrent writers interleaving " +
                    "forward advances with stale lower-ordinal writes (monotonic CAS-on-ordinal, not blind LWW / INV-1): " +
                    string.Join(" | ", observedRegressions));
                Assert.That(finalStatus!.Status, Is.EqualTo(UpdateProgress.Completed),
                    "the highest advanced ordinal wins and stale Queued writes never regress the terminal state");
            }
        }

        [Test]
        public async Task Requeue_OnlyActsOnAnAdmittedKey_NeverResurrectsARemovedOne()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var admitted = new UpdateKey(UpdateType.Team, 12);
            var removed = new UpdateKey(UpdateType.Team, 13);
            var store = new RedisUpdateStatusStore(multiplexer);

            store.TryAdmit(admitted, new UpdateStatus { UpdateType = UpdateType.Team, Id = 12, Status = UpdateProgress.Queued });
            store.Advance(admitted, UpdateProgress.Completed);

            store.Requeue(admitted);
            store.Requeue(removed);

            store.TryGet(admitted, out var requeued);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requeued!.Status, Is.EqualTo(UpdateProgress.Queued),
                    "the shared hash must accept the follow-up's reset past the monotonic Advance guard, otherwise a coalesced re-run would be invisible to every pod");
                Assert.That(store.TryGet(removed, out _), Is.False,
                    "the HEXISTS guard keeps a re-queue on a key another pod already removed from creating a phantom active entry nothing will ever complete");
            }
        }

        [Test]
        [Category("requires-docker")]
        public async Task HasQueuedWork_WhereTheRecordOfWorkInFlightIsKeptOutsideTheApplication_GivesTheSameAnswerInOneBatchedRead()
        {
            await using var redis = await RedisContainerFixture.StartFreshAsync();
            await using var multiplexer = await ConnectionMultiplexer.ConnectAsync(redis.GetConnectionString());

            var stores = StoresComparedAgainstEachOther.Select(entry => entry.Build(multiplexer)).ToArray();
            var acrossPods = stores.OfType<RedisUpdateStatusStore>().Single();

            foreach (var store in stores)
            {
                store.TryAdmit(KeyWaitingToStart, new UpdateStatus { UpdateType = UpdateType.Team, Id = 21, Status = UpdateProgress.Queued });
                store.TryAdmit(KeyAlreadyRunning, new UpdateStatus { UpdateType = UpdateType.Team, Id = 22, Status = UpdateProgress.Queued });
                store.Advance(KeyAlreadyRunning, UpdateProgress.InProgress);
            }

            var disagreements = QueuedLookups
                .Where(lookup => stores.Select(store => store.HasQueuedWork(lookup.Keys)).Distinct().Count() > 1)
                .Select(lookup => lookup.Description)
                .ToArray();

            var probeOptions = ConfigurationOptions.Parse(redis.GetConnectionString());
            probeOptions.AllowAdmin = true;
            await using var probe = await ConnectionMultiplexer.ConnectAsync(probeOptions);

            var server = probe.GetServer(probe.GetEndPoints()[0]);
            server.Execute("CONFIG", "RESETSTAT");

            var answeredQueued = acrossPods.HasQueuedWork([KeyNobodyAdmitted, KeyWaitingToStart]);

            var commandStats = server.InfoRaw("commandstats") ?? string.Empty;
            var wholeHashReads = CallsFor(commandStats, "hgetall") + CallsFor(commandStats, "hvals") + CallsFor(commandStats, "hkeys");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disagreements, Is.Empty,
                    "a deployment that keeps the record of work in flight outside the application must debounce identically to one that keeps it in memory, otherwise the same forecast fires at a different moment depending on how Lighthouse is deployed. Disagreed on: " +
                    string.Join(" | ", disagreements));
                Assert.That(answeredQueued, Is.True,
                    "one named key still waiting to start is enough to report queued work, so the agreement above is not agreement on a blanket no");
                Assert.That(CallsFor(commandStats, "hmget"), Is.EqualTo(1),
                    "the named keys are read in a single batched request, so the cost stays one round trip however many teams a portfolio has");
                Assert.That(wholeHashReads, Is.Zero,
                    "reading the whole hash would make a question about a handful of named keys cost as much as every update the entire installation is running");
            }
        }

        private static int CallsFor(string commandStats, string command)
        {
            var marker = $"cmdstat_{command}:calls=";
            var markerStart = commandStats.IndexOf(marker, StringComparison.Ordinal);
            if (markerStart < 0)
            {
                return 0;
            }

            var count = commandStats[(markerStart + marker.Length)..];
            var countEnd = count.IndexOf(',');

            return int.Parse(countEnd < 0 ? count : count[..countEnd], CultureInfo.InvariantCulture);
        }
    }
}
