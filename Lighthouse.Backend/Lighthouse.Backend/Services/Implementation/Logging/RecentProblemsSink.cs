using Lighthouse.Backend.Models.Logging;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
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

        private const string UpdateTypeProperty = "UpdateType";

        private const string IdProperty = "Id";

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
                logEvent.Exception?.GetType().Name)
            {
                Refresh = RefreshOf(logEvent),
            };

        /// <summary>
        /// What the event was a refresh of, for the few events that are about one. Only the reference is
        /// taken, never the name: a name copied in here would be the name the thing had when it broke, and
        /// somebody reading about it after a rename has to see what it is called now.
        /// </summary>
        private static RefreshSubject? RefreshOf(LogEvent logEvent)
        {
            if (!TryReadProperty(logEvent, UpdateTypeProperty, out UpdateType kind)
                || !TryReadProperty(logEvent, IdProperty, out int id))
            {
                return null;
            }

            var asWritten = HowTheLinePointsAtIt(logEvent);

            return asWritten.Length == 0 ? null : new RefreshSubject(kind, id, asWritten);
        }

        private static bool TryReadProperty<T>(LogEvent logEvent, string name, out T value)
        {
            if (logEvent.Properties.TryGetValue(name, out var captured) && captured is ScalarValue { Value: T scalar })
            {
                value = scalar;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// The stretch of the sentence that stands for the thing being refreshed: from where the line names
        /// what kind of thing it is to where it gives the number, along with whatever wording it puts
        /// between the two. Worked out here, where the unrendered template is still to hand, so that
        /// swapping a name into the sentence later is a search for a piece of text and nothing more.
        /// </summary>
        private static string HowTheLinePointsAtIt(LogEvent logEvent)
        {
            var tokens = logEvent.MessageTemplate.Tokens.ToArray();
            var kindAt = Array.FindIndex(tokens, token => token is PropertyToken { PropertyName: UpdateTypeProperty });
            var numberAt = Array.FindIndex(tokens, token => token is PropertyToken { PropertyName: IdProperty });

            if (kindAt < 0 || numberAt < kindAt)
            {
                return string.Empty;
            }

            using var howItIsWritten = new StringWriter(CultureInfo.InvariantCulture);

            for (var index = kindAt; index <= numberAt; index++)
            {
                tokens[index].Render(logEvent.Properties, howItIsWritten, CultureInfo.InvariantCulture);
            }

            return howItIsWritten.ToString();
        }

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
