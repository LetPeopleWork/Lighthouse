using System.Collections.Concurrent;
using Lighthouse.Backend.Cache;

namespace Lighthouse.Backend.Tests.Cache
{
    [TestFixture]
    public class CacheTest
    {
        private static readonly TimeSpan LongExpiry = TimeSpan.FromMinutes(5);

        [Test]
        [CancelAfter(5000)]
        public async Task ConcurrentInsertsOnUniqueKeys_DoNotThrow_AndAllValuesAreRetrievable()
        {
            const int threadCount = 64;
            var cache = new Cache<string, string>();
            var exceptions = new ConcurrentBag<Exception>();
            using var barrier = new Barrier(threadCount);

            var tasks = Enumerable.Range(0, threadCount).Select(i => Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    cache.Store($"key-{i}", $"value-{i}", LongExpiry);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exceptions, Is.Empty, "No thread should observe an exception");
                for (var i = 0; i < threadCount; i++)
                {
                    Assert.That(cache.Get($"key-{i}"), Is.EqualTo($"value-{i}"), $"key-{i} should be retrievable");
                }
                Assert.That(cache.Keys.ToList(), Has.Count.EqualTo(threadCount));
            }
        }

        [Test]
        [CancelAfter(5000)]
        public async Task ConcurrentReadersAndWriters_DoNotObserveCorruptedEntries()
        {
            const int writerThreads = 32;
            const int readerThreads = 32;
            const int keyPoolSize = 16;
            var cache = new Cache<string, string>();
            var exceptions = new ConcurrentBag<Exception>();
            var torn = new ConcurrentBag<string>();
            var validValues = new HashSet<string>();
            for (var k = 0; k < keyPoolSize; k++)
            {
                for (var v = 0; v < 64; v++)
                {
                    validValues.Add($"v-{k}-{v}");
                }
            }

            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            using var barrier = new Barrier(writerThreads + readerThreads);

            var writers = Enumerable.Range(0, writerThreads).Select(t => Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    var v = 0;
                    while (!stop.IsCancellationRequested)
                    {
                        var keyIndex = (t + v) % keyPoolSize;
                        var valueIndex = v % 64;
                        cache.Store($"k-{keyIndex}", $"v-{keyIndex}-{valueIndex}", LongExpiry);
                        v++;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

            var readers = Enumerable.Range(0, readerThreads).Select(t => Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    var v = 0;
                    while (!stop.IsCancellationRequested)
                    {
                        var keyIndex = (t + v) % keyPoolSize;
                        var observed = cache.Get($"k-{keyIndex}");
                        if (observed is not null && !validValues.Contains(observed))
                        {
                            torn.Add(observed);
                        }
                        v++;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

            await Task.WhenAll(writers.Concat(readers));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exceptions, Is.Empty, "No thread should observe an exception");
                Assert.That(torn, Is.Empty, "Readers should never observe a torn or partial value");
            }
        }

        [Test]
        [CancelAfter(5000)]
        public async Task ConcurrentGetOnExpiredKey_SerialisesLazyRemove_WithoutThrowing()
        {
            const int readerThreads = 32;
            var cache = new Cache<string, string>();
            cache.Store("metric:expired", "stale", TimeSpan.FromMilliseconds(10));
            await Task.Delay(50);

            var exceptions = new ConcurrentBag<Exception>();
            var nonDefaultResults = new ConcurrentBag<string>();
            using var barrier = new Barrier(readerThreads);

            var tasks = Enumerable.Range(0, readerThreads).Select(_ => Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    var observed = cache.Get("metric:expired");
                    if (observed is not null)
                    {
                        nonDefaultResults.Add(observed);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

            await Task.WhenAll(tasks);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exceptions, Is.Empty, "No thread should observe an exception during lazy remove");
                Assert.That(nonDefaultResults, Is.Empty, "Every reader of an expired entry must get the default value");
                Assert.That(cache.Keys.ToList(), Does.Not.Contain("metric:expired"));
            }
        }

        [Test]
        [CancelAfter(5000)]
        public async Task KeysEnumerationDuringMutation_DoesNotThrow_CollectionModified()
        {
            const int keyPoolSize = 100;
            const int snapshotCount = 200;
            var cache = new Cache<string, string>();
            var exceptions = new ConcurrentBag<Exception>();
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            var writer = Task.Run(() =>
            {
                try
                {
                    var i = 0;
                    while (!stop.IsCancellationRequested)
                    {
                        var key = $"churn-{i % keyPoolSize}";
                        if (i % 2 == 0)
                        {
                            cache.Store(key, $"v-{i}", LongExpiry);
                        }
                        else
                        {
                            cache.Remove(key);
                        }
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            try
            {
                for (var snapshot = 0; snapshot < snapshotCount; snapshot++)
                {
                    var keys = cache.Keys.Where(k => k.StartsWith("churn-", StringComparison.Ordinal)).ToList();
                    foreach (var key in keys)
                    {
                        Assert.That(key, Does.StartWith("churn-"));
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
            finally
            {
                stop.Cancel();
                await writer;
            }

            Assert.That(exceptions, Is.Empty,
                "No InvalidOperationException or IndexOutOfRangeException should escape Keys enumeration under concurrent mutation");
        }

        [Test]
        [CancelAfter(5000)]
        public async Task InvalidateMetricsStylePrefixRemoval_IsSafeUnderRacingMutations()
        {
            const string entityPrefix = "42_";
            const int initialEntries = 50;
            const int backgroundThreads = 16;
            var cache = new Cache<string, object>();
            for (var i = 0; i < initialEntries; i++)
            {
                cache.Store($"{entityPrefix}metric-{i}", new object(), LongExpiry);
            }

            var exceptions = new ConcurrentBag<Exception>();
            using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
            using var barrier = new Barrier(backgroundThreads + 1);

            var background = Enumerable.Range(0, backgroundThreads).Select(t => Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    var i = 0;
                    while (!stop.IsCancellationRequested)
                    {
                        cache.Store($"{entityPrefix}metric-{(t * 1000) + i}", new object(), LongExpiry);
                        _ = cache.Get($"{entityPrefix}metric-{i % initialEntries}");
                        i++;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToArray();

            var invalidator = Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    while (!stop.IsCancellationRequested)
                    {
                        var keysToRemove = cache.Keys.Where(k => k.StartsWith(entityPrefix, StringComparison.Ordinal)).ToList();
                        foreach (var key in keysToRemove)
                        {
                            cache.Remove(key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            await Task.WhenAll(background);
            await invalidator;

            var finalSweep = cache.Keys.Where(k => k.StartsWith(entityPrefix, StringComparison.Ordinal)).ToList();
            foreach (var key in finalSweep)
            {
                cache.Remove(key);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(exceptions, Is.Empty, "Invalidate + concurrent mutations must not throw");
                Assert.That(cache.Keys.Where(k => k.StartsWith(entityPrefix, StringComparison.Ordinal)),
                    Is.Empty,
                    "After invalidation + background settle, no entityPrefix keys may remain");
            }
        }

        [Test]
        public void StoreGetRemoveGet_PreservesSingleThreadedSemantics()
        {
            var cache = new Cache<string, string>();

            cache.Store("a", "1", TimeSpan.FromMinutes(1));
            var firstGet = cache.Get("a");
            cache.Remove("a");
            var secondGet = cache.Get("a");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(firstGet, Is.EqualTo("1"));
                Assert.That(secondGet, Is.Null);
                Assert.That(cache.Keys, Is.Empty);
            }
        }

        [Test]
        public async Task ExpiryOnRead_StillRemovesEntry_SingleThreaded()
        {
            var cache = new Cache<string, string>();
            cache.Store("temp", "v", TimeSpan.FromMilliseconds(10));
            await Task.Delay(50);

            var result = cache.Get("temp");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Null);
                Assert.That(cache.Keys.ToList(), Does.Not.Contain("temp"));
            }
        }
    }
}
