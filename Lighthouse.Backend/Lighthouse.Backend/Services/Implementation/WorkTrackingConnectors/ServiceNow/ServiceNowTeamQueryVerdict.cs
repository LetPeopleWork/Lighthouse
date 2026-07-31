using System.Net;
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
        /// <summary>The settings field the query verdicts send the flow coach back to.</summary>
        private const string QueryFieldName = "DataRetrievalValue";

        /// <summary>The settings field the kind-of-work verdicts send them to instead.</summary>
        private const string KindsOfWorkFieldName = "WorkItemTypes";

        /// <summary>
        /// Rung 0c — the team has not said which kinds of work are its own. Pre-flight, no IO.
        /// </summary>
        /// <remarks>
        /// ADR-123 decision 4. This is the missing-query rule on the kind-of-work dimension, and it
        /// lives here as well as in the schema flag because <c>isWorkItemTypesRequired</c> is a hint
        /// to the web UI while <c>PUT /api/teams/{id}</c> also serves the CLI and the MCP server,
        /// neither of which reads the schema.
        /// </remarks>
        public static ConnectionValidationResult FromMissingWorkItemTypes()
        {
            return ConnectionValidationResult.Failure(
                "missing_work_item_types",
                "This team has not said which kinds of work are its own, so it would read nothing. Enter them as work item types, using the system names ServiceNow stores — 'change_request', not 'Change Request'.",
                // Stryker disable once String: a support-log restatement of the message above, which is
                // what the flow coach acts on. Nothing branches on this line.
                "The team named no work item types, and a ServiceNow read is always scoped to them.",
                KindsOfWorkFieldName);
        }

        /// <summary>
        /// The four-rung readability ladder for one named kind of work, read off its own table
        /// (ADR-124 decision 2). A verdict that is valid means the class is readable, or exists and
        /// holds nothing — which is a legitimate configuration, not a failure.
        /// </summary>
        /// <param name="recordClass">The class name the flow coach typed.</param>
        /// <param name="statusCode">What <c>/api/now/table/{recordClass}?sysparm_limit=1</c> answered.</param>
        /// <param name="carriesRecords">
        /// Whether the body parsed <i>and</i> carried a record set. A success whose JSON has no
        /// <c>result</c> array — an error envelope a gateway rewrote into a 200, a sign-in page — is
        /// no evidence that the class is readable, so it is read as "answered, returned no data"
        /// exactly as the sync's own <c>RecordsFrom</c> reads it. Passing a class on it is how a
        /// misspelt name reaches a team that then syncs a subset in silence.
        /// </param>
        /// <param name="recordsTheInstanceHolds">
        /// <c>X-Total-Count</c>, which ServiceNow reports without consulting the ACLs it just applied,
        /// or <c>null</c> where the header was absent. The gap between this number and
        /// <paramref name="visibleRowCount"/> is the single mechanism AC-B6 rests on, so a probe
        /// without it has measured nothing and must say so rather than pass.
        /// </param>
        /// <param name="visibleRowCount">Rows the account actually got back.</param>
        public static ConnectionValidationResult FromClassProbe(
            string recordClass, HttpStatusCode statusCode, bool carriesRecords, int? recordsTheInstanceHolds, int visibleRowCount)
        {
            if (statusCode != HttpStatusCode.OK || !carriesRecords)
            {
                // Rungs 1 and 2 are the connection ladder's 400 and 403 with a class name where the
                // table name went — a class IS a table, so the messages are already right.
                return AboutTheKindOfWork(
                    ServiceNowValidationVerdict.FromResponse(statusCode, carriesRecords, visibleRowCount, recordClass));
            }

            if (recordsTheInstanceHolds is null)
            {
                return AboutTheKindOfWork(FromUncountableResultSet(recordClass));
            }

            // Rung 4. The gap between what the instance holds and what the account can see is the
            // only signal there is: an ACL-filtered read and a correct one are otherwise the same
            // response with fewer rows in it. Header = 0 is rung 5 — the class is simply empty.
            if (recordsTheInstanceHolds > 0 && visibleRowCount < 1)
            {
                return RecordsNotVisible(recordClass);
            }

            return ConnectionValidationResult.Success();
        }

        /// <summary>
        /// The second half of the class ladder (ADR-124 decision 2, amended 2026-07-31): what
        /// <c>/api/now/table/{table}?sysparm_query=sys_class_name={recordClass}</c> answered.
        /// </summary>
        /// <remarks>
        /// <see cref="FromClassProbe"/> establishes "this name is a readable table on this instance".
        /// The read needs the strictly stronger "records whose own class is this name are readable
        /// <i>under the table this connection is rooted at</i>", and only this probe measures it.
        /// Measured on the PDI: a team rooted at <c>incident</c> naming <c>change_request</c> passes
        /// the first probe (105 rows on its own table) and answers <c>header = 0</c> here — it would
        /// have synced nothing of that kind and said nothing about it.
        /// <para>
        /// Only reached for a class the instance holds records of somewhere, which is what makes
        /// <c>header = 0</c> mean "not under this table" rather than "empty everywhere".
        /// </para>
        /// </remarks>
        /// <param name="recordClass">The class name the flow coach typed.</param>
        /// <param name="table">The table this connection reads, and the one the probe addressed.</param>
        /// <param name="statusCode">What the instance answered.</param>
        /// <param name="carriesRecords">Whether the body parsed and carried a record set.</param>
        /// <param name="recordsTheInstanceHolds">
        /// <c>X-Total-Count</c> for that class under that table, or <c>null</c> where the header was
        /// absent. Measured ACL-blind through a class-scoped <c>sysparm_query</c> as well as at table
        /// granularity: <c>/task?sys_class_name=problem</c> reports 24 to an account shown none of them.
        /// </param>
        /// <param name="visibleRowCount">Rows the account actually got back.</param>
        public static ConnectionValidationResult FromClassUnderTableProbe(
            string recordClass,
            string table,
            HttpStatusCode statusCode,
            bool carriesRecords,
            int? recordsTheInstanceHolds,
            int visibleRowCount)
        {
            if (statusCode != HttpStatusCode.OK || !carriesRecords)
            {
                // The table is the connection's, so the ladder names the table rather than the class.
                return AboutTheKindOfWork(
                    ServiceNowValidationVerdict.FromResponse(statusCode, carriesRecords, visibleRowCount, table));
            }

            if (recordsTheInstanceHolds is null)
            {
                return AboutTheKindOfWork(FromUncountableResultSet(table));
            }

            if (recordsTheInstanceHolds < 1)
            {
                return NotUnderTheTable(recordClass, table);
            }

            if (visibleRowCount < 1)
            {
                return RecordsNotVisible(recordClass);
            }

            return ConnectionValidationResult.Success();
        }

        // Neither "this kind of work does not exist" nor "it is empty" — the probe on its own table
        // has already ruled both out, each with a better message. What is left is a real, populated
        // class whose records live somewhere else in the instance, which is what a flow coach reaches
        // by naming a sibling of the table their connection reads.
        private static ConnectionValidationResult NotUnderTheTable(string recordClass, string table)
        {
            return ConnectionValidationResult.Failure(
                "class_not_under_configured_table",
                $"'{recordClass}' is a kind of work this instance holds, but none of it lives under '{table}' — the table this connection reads. This team would sync nothing of that kind, and say nothing about it. Either name only kinds of work that '{table}' covers, or point the connection at a table they all sit under.",
                // Stryker disable once String: the two ways out are named in the message above, which
                // is what the flow coach acts on. This repeats the counts for a support log.
                $"'{table}' reported no records of the kind '{recordClass}' in X-Total-Count, while '{recordClass}' itself holds some.",
                KindsOfWorkFieldName);
        }

        // Suspicion, not proof: rows all filtered out by row-level ACLs for legitimate reasons read
        // identically to a class-level denial, so the message names both causes rather than asserting
        // a certainty the platform cannot supply. Same house style as no_records_visible.
        private static ConnectionValidationResult RecordsNotVisible(string recordClass)
        {
            return ConnectionValidationResult.Failure(
                "class_records_not_visible",
                $"ServiceNow says it holds records of the kind '{recordClass}', but this account was shown none of them. Either it lacks read access to '{recordClass}' — grant the matching per-table role — or every one of those records is hidden from it by a record-level rule. Until one of those changes, this team would sync as though that kind of work did not exist.",
                // Stryker disable once String: both causes and the role to grant are named in the
                // message above, which is the half a flow coach acts on. This repeats the two counts
                // for a support log.
                $"ServiceNow reported records of '{recordClass}' in X-Total-Count and returned none of them.",
                KindsOfWorkFieldName);
        }

        // The connection ladder points at the connection's own field. A rung reached through a class
        // the flow coach typed has to send them back to the field they typed it in.
        private static ConnectionValidationResult AboutTheKindOfWork(ConnectionValidationResult verdict)
        {
            verdict.FieldName = KindsOfWorkFieldName;

            return verdict;
        }

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
