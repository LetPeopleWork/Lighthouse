using System.Collections.Concurrent;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Microsoft.Extensions.Logging;
using Moq;

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

        [Test]
        public void Disposing_the_lane_runs_what_was_already_given_to_it_and_joins_every_thread_before_it_returns()
        {
            var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = 4 });
            using var holdTheFirst = new ManualResetEventSlim(initialState: false);
            var ranOn = new ConcurrentBag<Thread>();
            var first = RunOn(lane, () =>
            {
                ranOn.Add(Thread.CurrentThread);
                holdTheFirst.Wait(TheLongestAWaitMayTake);
            });
            var queuedBehindIt = Enumerable.Range(0, 6).Select(_ => RunOn(lane, () => ranOn.Add(Thread.CurrentThread))).ToList();

            var disposing = OnItsOwnThread(lane.Dispose);
            holdTheFirst.Set();
            var disposeReturned = disposing.Wait(TheLongestAWaitMayTake);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposeReturned, Is.True, "Disposing the lane did not return.");
                Assert.That(Task.WaitAll([first, .. queuedBehindIt], TimeSpan.Zero), Is.True, "Work given to the lane before it was disposed did not run.");
                Assert.That(ranOn, Has.None.Matches<Thread>(thread => thread.IsAlive), "Dispose returned while a lane thread was still running.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Work_given_to_the_lane_after_it_was_disposed_is_refused(bool theLaneHadRunSomething)
        {
            var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = 2 });
            if (theLaneHadRunSomething)
            {
                RunOn(lane, () => { }).Wait(TheLongestAWaitMayTake);
            }

            lane.Dispose();

            Assert.Throws<TaskSchedulerException>(() => RunOn(lane, () => { }));
        }

        [Test]
        public void Disposing_the_lane_twice_is_harmless()
        {
            var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = 2 });
            RunOn(lane, () => { }).Wait(TheLongestAWaitMayTake);

            lane.Dispose();

            Assert.DoesNotThrow(lane.Dispose);
        }

        [Test]
        public void A_thread_still_busy_when_the_lane_is_disposed_is_reported_and_left_to_end_with_the_process()
        {
            var logger = new Mock<ILogger<ForecastLane>>();
            var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = 2 }).ReportingThreadsThatDidNotFinishTo(logger.Object);
            using var started = new ManualResetEventSlim(initialState: false);
            using var letItGo = new ManualResetEventSlim(initialState: false);
            Thread? busy = null;
            var stuck = RunOn(lane, () =>
            {
                busy = Thread.CurrentThread;
                started.Set();
                letItGo.Wait(TheLongestAWaitMayTake * 3);
            });
            started.Wait(TheLongestAWaitMayTake);

            var disposeReturned = OnItsOwnThread(lane.Dispose).Wait(TheLongestAWaitMayTake);
            var stillBusy = busy is { IsAlive: true };
            var inTheBackground = stillBusy && busy!.IsBackground;
            letItGo.Set();
            stuck.Wait(TheLongestAWaitMayTake);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposeReturned, Is.True, "Disposing the lane waited for a busy thread without end.");
                Assert.That(stillBusy, Is.True, "The thread was meant to still be busy when the lane gave up waiting.");
                Assert.That(inTheBackground, Is.True, "A thread left busy would hold the process open.");
                logger.Verify(
                    l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
        }

        [Test]
        public void Disposing_the_lane_waits_for_a_busy_thread_that_ends_in_time_and_reports_nothing()
        {
            var logger = new Mock<ILogger<ForecastLane>>();
            var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = 2 }).ReportingThreadsThatDidNotFinishTo(logger.Object);
            using var started = new ManualResetEventSlim(initialState: false);
            using var letItGo = new ManualResetEventSlim(initialState: false);
            Thread? busy = null;
            var work = RunOn(lane, () =>
            {
                busy = Thread.CurrentThread;
                started.Set();
                letItGo.Wait(TheLongestAWaitMayTake);
            });
            started.Wait(TheLongestAWaitMayTake);

            var disposing = OnItsOwnThread(lane.Dispose);
            var returnedWhileTheThreadWasBusy = disposing.Wait(LongEnoughForAFreeThreadToPickItUp);
            letItGo.Set();
            var disposeReturned = disposing.Wait(TheLongestAWaitMayTake);
            var stillRunningAfterDispose = busy is { IsAlive: true };
            work.Wait(TheLongestAWaitMayTake);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(returnedWhileTheThreadWasBusy, Is.False, "Disposing the lane gave up on a thread without waiting for it.");
                Assert.That(disposeReturned, Is.True, "Disposing the lane did not return once its thread had ended.");
                Assert.That(stillRunningAfterDispose, Is.False, "Dispose returned while the lane thread was still running.");
                logger.Verify(
                    l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Never);
            }
        }

        [Test]
        public void Disposing_the_lane_waits_for_all_its_busy_threads_together_rather_than_one_after_another()
        {
            var logger = new Mock<ILogger<ForecastLane>>();
            var lane = new ForecastLane(new ForecastSimulationLimits(1, 1) { CoresSeen = 4 }).ReportingThreadsThatDidNotFinishTo(logger.Object);
            using var bothStarted = new CountdownEvent(2);
            using var letThemGo = new ManualResetEventSlim(initialState: false);
            var stuck = Enumerable.Range(0, 2)
                .Select(_ => RunOn(lane, () =>
                {
                    bothStarted.Signal();
                    letThemGo.Wait(TheLongestAWaitMayTake * 3);
                }))
                .ToList();
            bothStarted.Wait(TheLongestAWaitMayTake);

            var disposing = OnItsOwnThread(lane.Dispose);
            var disposeEnded = disposing.ContinueWith(_ => { }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Wait(TheLongestAWaitMayTake);
            letThemGo.Set();
            Task.WaitAll([.. stuck], TheLongestAWaitMayTake);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(disposeEnded, Is.True, "Disposing the lane waited for its busy threads one after another.");
                Assert.That(disposing.Status, Is.EqualTo(TaskStatus.RanToCompletion), "Disposing the lane failed while it waited for its busy threads.");
                logger.Verify(
                    l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.Once);
            }
        }

        private static Task OnItsOwnThread(Action work)
            => Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        private static Task RunOn(ForecastLane lane, Action work)
            => Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.DenyChildAttach, lane.Scheduler);
    }
}
