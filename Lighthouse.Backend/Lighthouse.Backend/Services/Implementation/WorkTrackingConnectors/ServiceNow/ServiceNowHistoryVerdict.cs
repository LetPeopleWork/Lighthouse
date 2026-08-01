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
        /// <summary>Every connection reads the whole work hierarchy, so its teams decide.</summary>
        public const string PerTeamCode = "history_determined_per_team";

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
        /// What connection validation can say about transition history: nothing, because the question
        /// has no answer at that scope (ADR-123 decision 10).
        /// </summary>
        /// <remarks>
        /// <c>metric_definition</c> holds 0 rows for <c>table=task</c> — definitions attach to
        /// concrete classes only — so a definition read here would report a missing state metric and
        /// tell the administrator to activate one on the state field of <c>task</c>. That is advice
        /// which cannot be followed, and it contradicts what their teams will actually get. One
        /// request saved, one false statement not made. Deliberately not a new
        /// <see cref="ServiceNowHistoryAvailability"/> member: the enum is what
        /// <c>SupportsTransitionHistory</c> branches on, and connection validation does not write it.
        /// </remarks>
        public static ConnectionValidationResult HistoryIsDecidedPerTeam()
        {
            return ConnectionValidationResult.SuccessWith(
                PerTeamCode,
                $"The connection works. Lighthouse reads ServiceNow through '{ServiceNowReadScope.RootTable}', which holds every kind of record a team might work on, so whether Lighthouse can see when work started is decided by the kinds of work each team names rather than by this connection — activate a Field value duration metric definition on the state field of each of those, not on '{ServiceNowReadScope.RootTable}', which carries none.");
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
