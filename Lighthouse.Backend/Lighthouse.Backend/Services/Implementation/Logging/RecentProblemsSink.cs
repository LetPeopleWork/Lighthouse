using Lighthouse.Backend.Models.Logging;
using Lighthouse.Backend.Services.Interfaces;
using Serilog.Core;
using Serilog.Events;
using System.Globalization;

namespace Lighthouse.Backend.Services.Implementation.Logging
{
    /// <summary>
    /// Holds the most recent warning-or-worse events in memory, so that noticing something has broken
    /// does not depend on somebody deciding to go and open the log file. Once it is full, the oldest
    /// entry goes to make room for the newest.
    ///
    /// It sits inside the log pipeline, so every thread in the application writes to it while a request
    /// is reading it. Both ends therefore take the same lock.
    /// </summary>
    public sealed class RecentProblemsSink : ILogEventSink, IRecentProblems
    {
        /// <summary>
        /// Measured, not guessed: nine days of a real dev instance's logs hold between 1 and 39
        /// warning-or-worse events per day, median 7, so this covers about five of the busiest day
        /// observed. Overridable through <c>RecentProblems:Capacity</c>.
        /// </summary>
        public const int DefaultCapacity = 200;

        /// <summary>
        /// Who an event that never said which logger wrote it is attributed to. Serilog fills in the
        /// source context for a logger created for a type, and the host writes some of its startup lines
        /// without one — those are still Lighthouse complaining, and a blank column would read as a bug
        /// in this section rather than as an event nobody signed.
        /// </summary>
        private const string TheApplicationItself = "Lighthouse";

        private const string SourceContextProperty = "SourceContext";

        private readonly Queue<RecentProblem> retained = new();

        private readonly object gate = new();

        private readonly int capacity;

        public RecentProblemsSink(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

            this.capacity = capacity;
        }

        public void Emit(LogEvent logEvent)
        {
            ArgumentNullException.ThrowIfNull(logEvent);

            var problem = AsProblem(logEvent);

            lock (gate)
            {
                if (retained.Count == capacity)
                {
                    retained.Dequeue();
                }

                retained.Enqueue(problem);
            }
        }

        public IReadOnlyList<RecentProblem> MostRecentFirst()
        {
            lock (gate)
            {
                var problems = retained.ToArray();
                Array.Reverse(problems);

                return problems;
            }
        }

        private static RecentProblem AsProblem(LogEvent logEvent)
            => new(
                logEvent.Timestamp,
                logEvent.Level.ToString(),
                SourceOf(logEvent),
                logEvent.RenderMessage(CultureInfo.InvariantCulture),
                logEvent.Exception?.GetType().Name);

        /// <summary>
        /// The last segment of the source context, which is the same thing the console template already
        /// shows an operator. The namespace in front of it is near enough identical on every line and
        /// would push the part that differs off the end of a narrow row.
        /// </summary>
        private static string SourceOf(LogEvent logEvent)
        {
            if (!logEvent.Properties.TryGetValue(SourceContextProperty, out var property)
                || property is not ScalarValue { Value: string sourceContext })
            {
                return TheApplicationItself;
            }

            var shortName = sourceContext[(sourceContext.LastIndexOf('.') + 1)..];

            return shortName.Length == 0 ? TheApplicationItself : shortName;
        }
    }
}
