using System.Collections.Concurrent;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Forecast;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// Every participant here blocks on a latch, and each one is a lane thread of its own, so no shared pool
    /// thread is ever parked by these tests. Every wait is bounded.
    /// </summary>
    public class ForecastLaneTest
    {
        private static readonly TimeSpan TheLongestAWaitMayTake = TimeSpan.FromSeconds(10);

        private static readonly TimeSpan LongEnoughForAFreeThreadToPickItUp = TimeSpan.FromMilliseconds(200);

        private const string TheNameEveryLaneThreadCarries = "Lighthouse forecast";

        [TestCase(2)]
        [TestCase(4)]
        [TestCase(8)]
        public void It_has_as_many_named_background_threads_as_one_forecast_may_use_cores_and_runs_its_tasks_on_them(int cores)
        {
            var threadsItMayUse = Math.Max(1, cores / 2);
            using var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = cores });
            using var everyThreadIsBusy = new CountdownEvent(threadsItMayUse);
            using var letThemGo = new ManualResetEventSlim(initialState: false);
            using var oneMoreStarted = new ManualResetEventSlim(initialState: false);
            var ranOn = new ConcurrentDictionary<int, Thread>();

            var busy = Enumerable.Range(0, threadsItMayUse)
                .Select(_ => RunOn(lane, () =>
                {
                    ranOn.TryAdd(Environment.CurrentManagedThreadId, Thread.CurrentThread);
                    everyThreadIsBusy.Signal();
                    letThemGo.Wait(TheLongestAWaitMayTake);
                }))
                .ToList();
            var oneMore = RunOn(lane, () =>
            {
                ranOn.TryAdd(Environment.CurrentManagedThreadId, Thread.CurrentThread);
                oneMoreStarted.Set();
            });

            var allBusyAtOnce = everyThreadIsBusy.Wait(TheLongestAWaitMayTake);
            var oneMoreFoundAFreeThread = oneMoreStarted.Wait(LongEnoughForAFreeThreadToPickItUp);
            letThemGo.Set();
            var everyTaskRan = Task.WaitAll([.. busy, oneMore], TheLongestAWaitMayTake);

            var threads = ranOn.Values.ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(allBusyAtOnce, Is.True, $"The lane did not run {threadsItMayUse} tasks at once.");
                Assert.That(oneMoreFoundAFreeThread, Is.False, $"The lane ran more than {threadsItMayUse} tasks at once.");
                Assert.That(everyTaskRan, Is.True, "Not every task given to the lane ran.");
                Assert.That(threads, Has.Count.EqualTo(threadsItMayUse));
                Assert.That(threads, Has.None.Matches<Thread>(thread => thread.IsThreadPoolThread), "A lane task ran on the shared pool.");
                Assert.That(threads, Has.All.Matches<Thread>(thread => thread.IsBackground), "A lane thread would hold the process open.");
                Assert.That(threads, Has.All.Matches<Thread>(thread => thread.Name != null && thread.Name.StartsWith(TheNameEveryLaneThreadCarries, StringComparison.Ordinal)));
            }
        }

        [Test]
        public void A_lane_thread_waiting_on_work_still_queued_on_the_lane_runs_that_work_itself()
        {
            using var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = 1 });
            var innerRan = false;

            var outer = RunOn(lane, () =>
            {
                var inner = RunOn(lane, () => innerRan = true);

                // An unbounded wait is the one .NET offers to run inline, and the one a parallel loop makes on
                // its own queued parts; the outer wait below keeps the test bounded.
                inner.Wait();
            });

            var outerEnded = outer.Wait(TheLongestAWaitMayTake);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outerEnded, Is.True, "A lane thread waiting on its own queued work held the lane.");
                Assert.That(innerRan, Is.True);
            }
        }

        [Test]
        public void Disposing_the_lane_ends_its_threads()
        {
            var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = 4 });
            var ranOn = new ConcurrentBag<Thread>();
            var tasks = Enumerable.Range(0, 4).Select(_ => RunOn(lane, () => ranOn.Add(Thread.CurrentThread))).ToList();
            var everyTaskRan = Task.WaitAll([.. tasks], TheLongestAWaitMayTake);

            lane.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(everyTaskRan, Is.True);
                Assert.That(ranOn, Is.Not.Empty);
                Assert.That(ranOn, Has.None.Matches<Thread>(thread => thread.IsAlive), "A lane thread outlived the lane.");
            }
        }

        private static Task RunOn(ForecastLane lane, Action work)
            => Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.DenyChildAttach, lane.Scheduler);
    }
}
