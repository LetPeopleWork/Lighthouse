using System.Collections.Concurrent;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using StackExchange.Redis;

namespace Lighthouse.Backend.Tests.Integration.Containers
{
    [TestFixture]
    public class UpdateStatusStoreContainerTests
    {
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
    }
}
