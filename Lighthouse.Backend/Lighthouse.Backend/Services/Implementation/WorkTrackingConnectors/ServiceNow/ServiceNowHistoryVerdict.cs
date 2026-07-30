using System.Net;
using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // SCAFFOLD (DISTILL slice 04, Story #5577)
    //
    // ADR-118 decision 5. The second pure core, on ADR-114's pattern: three scalars in, a verdict out,
    // no IO. Two different things stop a team getting true time-in-progress, and — unlike the
    // rights-versus-empty case that forced the C-1 amendment in slice 01 — the platform CAN tell them
    // apart. Conflating them would repeat this epic's headline mistake in a new place.
    public static class ServiceNowHistoryVerdict
    {
        /// <summary>The account cannot read the metric tables at all.</summary>
        public const string NoRightsCode = "history_requires_itil";

        /// <summary>The account can read them, but the instance measures no state spans.</summary>
        public const string NoStateMetricCode = "history_requires_state_metric";

        private const string ScaffoldSentinel = "__scaffold__";

        /// <summary>
        /// Decides whether this instance can supply transition history, from the answer to the
        /// metric-definition read: the status it returned, and how many <c>Field value duration</c>
        /// definitions on the configured table came back.
        /// </summary>
        public static ServiceNowHistoryAvailability From(HttpStatusCode statusCode, int stateSpanDefinitions)
        {
            // The scaffold answers with the CONSERVATIVE wrong value on purpose. Returning Available
            // would be a scaffold that says history works whatever the instance answered — the same
            // success-costume shape slice 01 found in its own ValidateConnection scaffold, and the
            // one this epic exists to prevent. Being wrong towards "unavailable" cannot fake a
            // passing capability test.
            return ServiceNowHistoryAvailability.NoStateMetric;
        }

        /// <summary>
        /// The connection verdict an administrator sees. A missing capability is not a broken
        /// connection, so this rides a success (ADR-118 D5) and never fails validation.
        /// </summary>
        public static ConnectionValidationResult ToValidationResult(ServiceNowHistoryAvailability availability, string table)
        {
            // Failure rather than SuccessWith: a scaffold that already returns a valid result would
            // let the "an advisory never fails the connection" test pass before anything implements it.
            return ConnectionValidationResult.Failure(ScaffoldSentinel, ScaffoldSentinel);
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
