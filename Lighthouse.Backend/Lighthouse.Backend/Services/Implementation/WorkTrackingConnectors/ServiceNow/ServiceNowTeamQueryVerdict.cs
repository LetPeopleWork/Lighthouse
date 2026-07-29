using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// The functional core of team-settings validation, kept pure for the same reason
    /// <see cref="ServiceNowValidationVerdict"/> is (ADR-114).
    /// </summary>
    /// <remarks>
    /// The interesting rung is <see cref="FromTeamProbe"/>. ServiceNow drops a query term naming a
    /// field the table does not have and answers with the entire table — measured: with
    /// <c>sysparm_query=not_a_real_field=whatever</c> the API returned all 96 rows, identical to no
    /// query at all. A real field with an impossible value returns 0. Neither errors. A flow coach
    /// who fat-fingers a field name therefore gets metrics computed over every incident in the
    /// instance, looking plausible and being wrong.
    /// <para>
    /// The only detection available to a read-only account is comparing the filtered count against
    /// the unfiltered count. That comparison is <b>suspicion, not proof</b> — a query can legitimately
    /// select a whole table. So the message names both causes rather than asserting a certainty the
    /// platform cannot supply, which is the same shape as <c>no_records_visible</c> in slice 01.
    /// <c>sys_dictionary</c> would settle it, but its readability under <c>sn_*_read</c> has never
    /// been measured, and building on an unmeasured read is the works-for-admin trap the Q10
    /// correction just caught.
    /// </para>
    /// </remarks>
    public static class ServiceNowTeamQueryVerdict
    {
        // SCAFFOLD (DISTILL slice 02, Story #5575)
        private const string ScaffoldSentinel = "__scaffold__";

        /// <summary>
        /// Rung 0 — the team carries no ServiceNow query. Pre-flight, no IO.
        /// </summary>
        public static ConnectionValidationResult FromMissingQuery()
        {
            // SCAFFOLD (DISTILL slice 02, Story #5575)
            return ConnectionValidationResult.Failure(ScaffoldSentinel, ScaffoldSentinel);
        }

        /// <summary>
        /// Rungs 1-3 — the instance answered both probes. US-02 AC6.
        /// </summary>
        /// <param name="table">The configured table, named back to the user in every message.</param>
        /// <param name="matchedCount">Rows the configured query matched.</param>
        /// <param name="tableTotalCount">Rows the same table holds with no query at all.</param>
        public static ConnectionValidationResult FromTeamProbe(string table, int matchedCount, int tableTotalCount)
        {
            // SCAFFOLD (DISTILL slice 02, Story #5575)
            _ = table;
            _ = matchedCount;
            _ = tableTotalCount;

            return ConnectionValidationResult.Failure(ScaffoldSentinel, ScaffoldSentinel);
        }
    }
}
