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
        /// <summary>The settings field every one of these verdicts sends the flow coach back to.</summary>
        private const string QueryFieldName = "DataRetrievalValue";

        /// <summary>
        /// Rung 0 — the team carries no ServiceNow query. Pre-flight, no IO.
        /// </summary>
        public static ConnectionValidationResult FromMissingQuery()
        {
            return ConnectionValidationResult.Failure(
                "missing_query",
                "This team has not said which ServiceNow records are theirs. Enter the encoded query that selects them, for example 'assignment_group.name=Service Desk^active=true'.",
                // Stryker disable once String: a support-log restatement of the message above, which is
                // what the flow coach acts on and what ATeamThatHasNotSaidWhichWorkIsTheirs_IsAskedForAQuery
                // asserts. Nothing branches on this line.
                "The team carries no ServiceNow query, and asking the Table API without one returns the whole table.",
                QueryFieldName);
        }

        /// <summary>
        /// Rung 0b — the instance answered, but did not say how big the result set is.
        /// </summary>
        /// <remarks>
        /// The count probe asks for a single row, so the body can only ever say 0 or 1 and
        /// <c>X-Total-Count</c> is the sole source of the number both sides of the comparison rest
        /// on. Guessing when the header is absent makes matched and total both 1 for every team on
        /// the instance, which reads as a query that selects the whole table — a refusal that names
        /// the wrong cause, for every team, on every save. Naming the missing header instead sends
        /// the administrator at the proxy that stripped it.
        /// </remarks>
        public static ConnectionValidationResult FromUncountableResultSet(string table)
        {
            return ConnectionValidationResult.Failure(
                "result_size_unknown",
                $"ServiceNow did not report how many records '{table}' holds, so Lighthouse cannot tell whether this query was silently widened to the whole table. A proxy in front of the instance usually strips the X-Total-Count header this rests on. Let that header through, then validate again.",
                // Stryker disable once String: the header to let through is named in the message
                // above, which is the half an administrator acts on; this repeats it for a support log.
                $"The response for '{table}' carried no usable X-Total-Count header.",
                QueryFieldName);
        }

        /// <summary>
        /// Rungs 1-3 — the instance answered both probes. US-02 AC6.
        /// </summary>
        /// <param name="table">The configured table, named back to the user in every message.</param>
        /// <param name="matchedCount">Rows the configured query matched.</param>
        /// <param name="tableTotalCount">Rows the same table holds with no query at all.</param>
        public static ConnectionValidationResult FromTeamProbe(string table, int matchedCount, int tableTotalCount)
        {
            if (matchedCount < 1)
            {
                return NoWorkSelected(table);
            }

            if (matchedCount == tableTotalCount)
            {
                return QuerySelectsEverything(table, matchedCount);
            }

            return ConnectionValidationResult.Success();
        }

        // Zero is checked before the equality above on purpose: a table with nothing in it also has
        // matched == total, and telling an empty service desk that its query is too wide is an
        // accusation about the one thing that is definitely not wrong.
        private static ConnectionValidationResult NoWorkSelected(string table)
        {
            return ConnectionValidationResult.Failure(
                "no_work_items_found",
                $"This query selects no records in '{table}'. Either the query matches nothing, or the table holds nothing yet. Check the query against the fields and values '{table}' actually uses.",
                // Stryker disable once String: the correction an administrator makes is in the message
                // above; this only repeats the count for a support log.
                $"The query matched 0 rows in '{table}'.",
                QueryFieldName);
        }

        private static ConnectionValidationResult QuerySelectsEverything(string table, int matchedCount)
        {
            return ConnectionValidationResult.Failure(
                "query_matches_whole_table",
                $"This query selects every record in '{table}'. ServiceNow drops a query term naming a field the table does not have and answers with the whole table in silence, so either a field name is misspelled or this team genuinely is the entire table. Check the field names before saving.",
                $"The query matched {matchedCount} rows, and '{table}' holds {matchedCount} rows with no query at all.",
                QueryFieldName);
        }
    }
}
