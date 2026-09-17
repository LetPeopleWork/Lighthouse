using System.Net;

namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors
{
    /// <summary>
    /// A work tracking system read the query Lighthouse sent and refused to run it. Its sentence and the query
    /// it rejected are the only two things anyone can act on, so both travel on the exception: a status code
    /// alone never says what to change, and a warning logged deep inside a connector names neither the
    /// portfolio nor the team whose refresh stopped.
    ///
    /// It lives beside the other connector port types so a background refresh can recognise a refusal without
    /// knowing which work tracking system produced it.
    ///
    /// It is an <see cref="HttpRequestException"/> because callers along the sync path answer "the system
    /// could not be asked" by catching exactly that, and a refusal has always reached them that way.
    /// </summary>
    public class WorkTrackingRefusedException : HttpRequestException
    {
        /// <summary>
        /// Long enough to recognise a query by, short enough that a log line, a panel row and a validation
        /// message can all still carry the sentence next to it.
        /// </summary>
        private const int LongestReportedQuery = 500;

        public WorkTrackingRefusedException(string message, string rejectedQuery, HttpStatusCode statusCode)
            : base(message, null, statusCode)
        {
            RejectedQuery = ShortEnoughToReport(rejectedQuery);

            ExplanationNamesTheQuery = RejectedQuery.Length > 0
                && Message.Contains(RejectedQuery, StringComparison.Ordinal);
        }

        /// <summary>
        /// The query, already cut to what any surface can print. It is bounded here rather than by each
        /// caller because a second bound kept somewhere else drifts from this one, and then no reader can
        /// tell which screen obeys which.
        /// </summary>
        public string RejectedQuery { get; }

        /// <summary>
        /// A connector left with nothing but a status code has only the query to report, so it names the
        /// query inside its own sentence. Anyone composing prose around that sentence has to know, or one
        /// line ends up carrying the same query twice.
        /// </summary>
        public bool ExplanationNamesTheQuery { get; }

        /// <summary>
        /// A configuration narrowing on hundreds of projects or releases builds a query longer than a log
        /// line, a panel row or a validation message can show, and repeating all of it buries the status.
        /// Connectors call this before the exception exists, so their own sentence keeps to the same bound.
        /// </summary>
        public static string ShortEnoughToReport(string query)
        {
            if (query.Length <= LongestReportedQuery)
            {
                return query;
            }

            // Cutting between the two halves of one character leaves a half that cannot be written as UTF-8
            // at all, and this text travels onward as JSON - to a log sink, and to the settings screen.
            var cut = char.IsHighSurrogate(query[LongestReportedQuery - 1])
                ? LongestReportedQuery - 1
                : LongestReportedQuery;

            return string.Concat(query.AsSpan(0, cut), "…");
        }
    }
}
