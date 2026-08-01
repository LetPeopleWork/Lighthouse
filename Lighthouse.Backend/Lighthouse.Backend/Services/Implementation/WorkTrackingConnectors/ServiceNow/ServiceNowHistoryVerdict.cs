using System.Net;
using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// Whether a ServiceNow instance can supply transition history, and what an administrator would
    /// have to change if it cannot. Pure (ADR-114): scalars in, a verdict out, no IO.
    /// </summary>
    public static class ServiceNowHistoryVerdict
    {
        /// <summary>
        /// Decides whether this instance can supply transition history, from the answer to the
        /// metric-definition read: the status it returned, and which of the team's kinds of work a
        /// <c>Field value duration</c> definition came back for.
        /// </summary>
        /// <remarks>
        /// Coverage rather than a count (Bug #5621 F6). Definitions attach to concrete record classes
        /// and never to a base table (ADR-123 decision 9), so a team naming two kinds of work needs
        /// one on each — and an aggregate count above zero reported Available for a team where only
        /// the first was configured, leaving every record of the second kind with no dates and no
        /// warning. Whether a definition measures <em>state</em> is a question this cannot answer: the
        /// state field is named differently on each class, so it is settled by what the span read
        /// actually brings back.
        /// </remarks>
        public static ServiceNowHistoryAvailability From(
            HttpStatusCode statusCode,
            bool carriesRecords,
            IEnumerable<string> kindsOfWorkTheTeamNamed,
            IReadOnlyCollection<string> kindsOfWorkADefinitionCameBackFor)
        {
            // A refusal outranks the count: zero definitions came back because the read was refused,
            // not because none exist, and the two remedies are different (ADR-118 D5).
            if (statusCode == HttpStatusCode.Forbidden)
            {
                return ServiceNowHistoryAvailability.NoRights;
            }

            // An unrecognised answer is not evidence that history works. It resolves to the remedy an
            // administrator can verify before acting on it — rationale in the commit for step 04-03.
            if (statusCode != HttpStatusCode.OK)
            {
                return ServiceNowHistoryAvailability.NoStateMetric;
            }

            // A 200 that carried no record set at all is not evidence either (Bug #5621). The
            // instance said yes and returned a sign-in page, so nothing came back -- and reporting
            // Available off it would declare history supported on the strength of an answer nobody
            // could read.
            if (!carriesRecords)
            {
                return ServiceNowHistoryAvailability.NoStateMetric;
            }

            var everyKindIsMeasured = kindsOfWorkTheTeamNamed
                .All(kindOfWork => kindsOfWorkADefinitionCameBackFor.Contains(kindOfWork, StringComparer.OrdinalIgnoreCase));

            return everyKindIsMeasured && kindsOfWorkADefinitionCameBackFor.Count > 0
                ? ServiceNowHistoryAvailability.Available
                : ServiceNowHistoryAvailability.NoStateMetric;
        }

        /// <summary>
        /// The verdict for an answer that could not be read at all — a refusal, or a 200 whose body
        /// carried no record set (the sign-in page ADR-114 exists for).
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="From"/> because coverage is not the question here and answering
        /// it would mean passing empty collections that read as "the team named no kinds of work".
        /// </remarks>
        public static ServiceNowHistoryAvailability FromAnUnreadableAnswer(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.Forbidden
                ? ServiceNowHistoryAvailability.NoRights
                : ServiceNowHistoryAvailability.NoStateMetric;
        }

        /// <summary>
        /// Which of the team's kinds of work returned span rows that were none of them states.
        /// </summary>
        /// <remarks>
        /// Bug #5630. <see cref="From"/> asks whether a <c>field_value_duration</c> definition came
        /// back per class; this asks whether one of them measures <em>state</em>, which is a question
        /// only the spans can answer — stock <c>change_request</c> carries those definitions on
        /// <c>approval</c> and <c>type</c> and none on <c>state</c>, and the definition read cannot
        /// tell the two apart. Rows that arrived and were all discarded are the evidence; no rows are
        /// not, for the reason the whole-team guard already gives — a class whose records have not
        /// moved since the definition was activated is a quiet class, not a broken one.
        /// </remarks>
        public static IReadOnlyList<string> KindsOfWorkMeasuredByNothingOnState(
            IEnumerable<string> kindsOfWorkTheTeamNamed,
            IReadOnlyCollection<string> kindsOfWorkThatReturnedSpans,
            IReadOnlyCollection<string> kindsOfWorkWhoseSpansTheTeamRecognises)
        {
            return
            [
                .. kindsOfWorkTheTeamNamed.Where(kindOfWork =>
                    kindsOfWorkThatReturnedSpans.Contains(kindOfWork, StringComparer.OrdinalIgnoreCase)
                    && !kindsOfWorkWhoseSpansTheTeamRecognises.Contains(kindOfWork, StringComparer.OrdinalIgnoreCase)),
            ];
        }
    }

    /// <summary>Whether this instance can supply transition history, and if not, why not.</summary>
    public enum ServiceNowHistoryAvailability
    {
        /// <summary>State spans are readable — Lighthouse reports true time-in-progress.</summary>
        Available,

        /// <summary>The metric tables are refused. The account needs an itil-grade role.</summary>
        NoRights,

        /// <summary>Readable, but nothing measures state spans on the team's kinds of work.</summary>
        NoStateMetric,
    }
}
