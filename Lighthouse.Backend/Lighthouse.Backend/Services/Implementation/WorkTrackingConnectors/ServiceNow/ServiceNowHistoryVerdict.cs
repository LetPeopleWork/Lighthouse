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
        /// metric-definition read: the status it returned, and how many <c>Field value duration</c>
        /// definitions on the team's kinds of work came back.
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
