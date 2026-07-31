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
        /// <summary>The account cannot read the metric tables at all.</summary>
        public const string NoRightsCode = "history_requires_itil";

        /// <summary>The account can read them, but the instance measures no state spans.</summary>
        public const string NoStateMetricCode = "history_requires_state_metric";

        /// <summary>The connection reads a table with several record classes, so its teams decide.</summary>
        public const string PerTeamCode = "history_determined_per_team";

        // ADR-117: whichever cause fired, the administrator has to learn which number they get meanwhile.
        private const string RequestToResolutionCaveat =
            "Until then, cycle time and work item age for this team are measured opened-to-resolution — from when the "
            + "record was raised to when it was closed — which reads longer than the time the team spent working on it.";

        /// <summary>
        /// Decides whether this instance can supply transition history, from the answer to the
        /// metric-definition read: the status it returned, and how many <c>Field value duration</c>
        /// definitions on the configured table came back.
        /// </summary>
        public static ServiceNowHistoryAvailability From(HttpStatusCode statusCode, int stateSpanDefinitions)
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

            return stateSpanDefinitions > 0
                ? ServiceNowHistoryAvailability.Available
                : ServiceNowHistoryAvailability.NoStateMetric;
        }

        /// <summary>
        /// What a connection rooted at a table with descendants can say about transition history:
        /// nothing, because the question has no answer at that scope (ADR-123 decision 10).
        /// </summary>
        /// <remarks>
        /// <c>metric_definition</c> holds 0 rows for <c>table=task</c> — definitions attach to
        /// concrete classes only — so the read below would return <see cref="NoStateMetricCode"/> and
        /// tell the administrator to activate a definition on the state field of <c>task</c>. That is
        /// advice which cannot be followed, and it contradicts what their teams will actually get.
        /// One request saved, one false statement not made, and deliberately not a new
        /// <see cref="ServiceNowHistoryAvailability"/> member: the enum is what
        /// <c>SupportsTransitionHistory</c> branches on, and connection validation does not write it.
        /// </remarks>
        public static ConnectionValidationResult ForHierarchyRoot(string table)
        {
            return ConnectionValidationResult.SuccessWith(
                PerTeamCode,
                $"The connection works. '{table}' holds several kinds of record, so whether Lighthouse can see when work started is decided by the kinds of work each team names rather than by this connection — activate a Field value duration metric definition on the state field of each of those, not on '{table}', which carries none.");
        }

        /// <summary>
        /// The connection verdict an administrator sees. A missing capability is not a broken
        /// connection, so this rides a success (ADR-118 D5) and never fails validation.
        /// </summary>
        public static ConnectionValidationResult ToValidationResult(ServiceNowHistoryAvailability availability, string table)
        {
            return availability switch
            {
                ServiceNowHistoryAvailability.NoRights => ConnectionValidationResult.SuccessWith(
                    NoRightsCode,
                    "The connection works, but ServiceNow refuses this account the metric tables, so Lighthouse cannot "
                    + "see when work started or stopped. Ask your ServiceNow administrator to grant the integration "
                    + "account the itil role, then validate the connection again to pick up true time in progress. "
                    + RequestToResolutionCaveat),

                ServiceNowHistoryAvailability.NoStateMetric => ConnectionValidationResult.SuccessWith(
                    NoStateMetricCode,
                    $"The connection works, but nothing on the {table} table measures how long a record spends in each "
                    + "state, so Lighthouse cannot see when work started or stopped. Activate a Field value duration "
                    + $"metric definition on the state field of {table}, then validate the connection again to pick up "
                    + "true time in progress. "
                    + RequestToResolutionCaveat),

                _ => ConnectionValidationResult.Success(),
            };
        }
    }

    /// <summary>Whether this instance can supply transition history, and if not, why not.</summary>
    public enum ServiceNowHistoryAvailability
    {
        /// <summary>State spans are readable — Lighthouse reports true time-in-progress.</summary>
        Available,

        /// <summary>The metric tables are refused. The account needs an itil-grade role.</summary>
        NoRights,

        /// <summary>Readable, but nothing measures state spans on the configured table.</summary>
        NoStateMetric,
    }
}
