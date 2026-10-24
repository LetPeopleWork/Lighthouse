using System.Collections.Concurrent;
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

        private int started;

        public ForecastLane(ForecastSimulationLimits limits)
        {
            scheduler = new OnTheLaneThreads(this);
            threads = [.. Enumerable.Range(1, limits.MostRunsAtOnce).Select(CreateThread)];
        }

        public TaskScheduler Scheduler => scheduler;

        private int ThreadCount => threads.Count;

        private bool HasStarted => Volatile.Read(ref started) == 1;

        /// <summary>
        /// Stops taking work, lets each thread finish what it holds, and waits a short while for them to end. A
        /// thread that is still busy after that is left to the process: the queue it reads stays open, so it
        /// is not torn from under it.
        /// </summary>
        public void Dispose()
        {
            waiting.CompleteAdding();

            var everyThreadEnded = !HasStarted || threads.TrueForAll(thread => thread.Join(HowLongAThreadMayTakeToFinish));

            if (everyThreadEnded)
            {
                waiting.Dispose();
            }
        }

        private Thread CreateThread(int number) => new(scheduler.RunWhatIsQueued)
        {
            IsBackground = true,
            Name = $"Lighthouse forecast {number}",
        };

        // The threads start with the first round rather than with the lane, so a host that never forecasts
        // never owns idle threads it may not get the chance to stop.
        private void StartTheThreadsOnce()
        {
            if (Interlocked.Exchange(ref started, 1) == 0)
            {
                threads.ForEach(thread => thread.Start());
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

            protected override void QueueTask(Task task)
            {
                lane.waiting.Add(task);
                lane.StartTheThreadsOnce();
            }

            // Waiting on a task that is still queued runs it right here when the waiter is a lane thread itself;
            // otherwise a round waiting on its own queued work could hold every lane thread and never move.
            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
                => lane.IsOneOfItsThreads(Thread.CurrentThread) && TryExecuteTask(task);

            protected override IEnumerable<Task> GetScheduledTasks() => lane.waiting.ToArray();
        }
    }
}
