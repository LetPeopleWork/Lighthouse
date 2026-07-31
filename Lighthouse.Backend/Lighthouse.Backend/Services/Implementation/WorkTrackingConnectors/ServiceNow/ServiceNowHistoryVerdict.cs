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
