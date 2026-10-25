using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    /// <summary>
    /// Threads of Lighthouse's own that background forecast rounds run on, so a long round never takes the
    /// shared pool threads that answer pages. There are as many as one forecast may use cores, and they are
    /// background threads, so they never keep the process alive when Lighthouse shuts down.
    /// </summary>
    public sealed class ForecastLane : IDisposable
    {
        private static readonly TimeSpan HowLongAThreadMayTakeToFinish = TimeSpan.FromSeconds(5);

        private readonly BlockingCollection<Task> waiting = new();

        private readonly List<Thread> threads;

        private readonly OnTheLaneThreads scheduler;

        // Handing work over (and starting the threads with the first of it) and closing the lane each happen whole
        // under this lock. Otherwise a stop could close and throw away the queue between the moment work was added
        // and the moment the threads started on it, or wait on a thread that had not been started yet.
        private readonly Lock handOver = new();

        private bool started;

        private bool disposed;

        private ILogger logger = NullLogger.Instance;

        public ForecastLane(ForecastSimulationLimits limits)
        {
            scheduler = new OnTheLaneThreads(this);
            threads = [.. Enumerable.Range(1, limits.MostRunsAtOnce).Select(CreateThread)];
        }

        public TaskScheduler Scheduler => scheduler;

        private int ThreadCount => threads.Count;

        /// <summary>Where the lane reports a thread that did not finish in time; until told, it reports nowhere.</summary>
        public ForecastLane ReportingThreadsThatDidNotFinishTo(ILogger<ForecastLane> reportTo)
        {
            logger = reportTo;
            return this;
        }

        /// <summary>
        /// Stops taking work, lets the threads run what they were already given, and waits a short while, all
        /// threads together, for them to end. A thread that is still busy after that is reported and left to the
        /// process: the queue it reads stays open, so it is not torn from under it.
        /// </summary>
        public void Dispose()
        {
            bool threadsWereStarted;

            lock (handOver)
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                waiting.CompleteAdding();
                threadsWereStarted = started;
            }

            var stillBusy = threadsWereStarted ? ThreadsStillBusyAfterWaiting() : [];

            if (stillBusy.Count == 0)
            {
                waiting.Dispose();
                return;
            }

            logger.LogWarning("{Count} forecast thread(s) were still busy {Seconds}s after Lighthouse began to stop and are left to end with the process: {Threads}",
                stillBusy.Count, HowLongAThreadMayTakeToFinish.TotalSeconds, string.Join(", ", stillBusy.Select(thread => thread.Name)));
        }

        private List<Thread> ThreadsStillBusyAfterWaiting()
        {
            var waited = Stopwatch.StartNew();

            return [.. threads.Where(thread => !thread.Join(TimeLeft(waited)))];
        }

        private static TimeSpan TimeLeft(Stopwatch waited)
        {
            var left = HowLongAThreadMayTakeToFinish - waited.Elapsed;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }

        private Thread CreateThread(int number) => new(scheduler.RunWhatIsQueued)
        {
            IsBackground = true,
            Name = $"Lighthouse forecast {number}",
        };

        // The threads start with the first round rather than with the lane, so a host that never forecasts
        // never owns idle threads it may not get the chance to stop.
        private void Take(Task task)
        {
            lock (handOver)
            {
                waiting.Add(task);

                if (!started)
                {
                    threads.ForEach(thread => thread.Start());
                    started = true;
                }
            }
        }

        private bool IsOneOfItsThreads(Thread thread) => threads.Contains(thread);

        private sealed class OnTheLaneThreads(ForecastLane lane) : TaskScheduler
        {
            public override int MaximumConcurrencyLevel => lane.ThreadCount;

            public void RunWhatIsQueued()
            {
                foreach (var task in lane.waiting.GetConsumingEnumerable())
                {
                    TryExecuteTask(task);
                }
            }

            protected override void QueueTask(Task task) => lane.Take(task);

            // Waiting on a task that is still queued runs it right here when the waiter is a lane thread itself;
            // otherwise a round waiting on its own queued work could hold every lane thread and never move.
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
                => lane.IsOneOfItsThreads(Thread.CurrentThread) && TryExecuteTask(task);

            protected override IEnumerable<Task> GetScheduledTasks() => lane.waiting.ToArray();
        }
    }
}
