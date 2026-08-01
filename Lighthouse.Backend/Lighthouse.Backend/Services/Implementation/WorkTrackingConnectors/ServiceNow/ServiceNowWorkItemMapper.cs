using System.Globalization;
using System.Text.Json;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// The functional core of the team sync (same shape as <see cref="ServiceNowValidationVerdict"/>,
    /// ADR-114): one ServiceNow record read with <c>sysparm_display_value=all</c> in, one
    /// <see cref="WorkItemBase"/> out. No IO, so every rule below is reachable as a unit test.
    /// </summary>
    /// <remarks>
    /// Two rules carry the whole slice, and both are invisible failures if broken.
    /// <para>
    /// <b>The date trap.</b> Under <c>sysparm_display_value=all</c> every field arrives as
    /// <c>{ display_value, value }</c>, where <c>value</c> is UTC and <c>display_value</c> is the
    /// instance timezone. They were measured seven hours apart, with <c>sys_created_on</c> crossing
    /// a date boundary between the two forms. Lighthouse buckets Throughput by day, so the rule is
    /// split by field kind: identifiers and instants come from <c>.value</c>, human-facing labels
    /// come from <c>.display_value</c>.
    /// </para>
    /// <para>
    /// <b>ADR-117, amended 2026-07-31.</b> These are the answers for a record whose transition
    /// history the instance cannot supply; where it can, the connector overrides both from the
    /// record's spans. <c>StartedDate</c> is <c>opened_at</c> falling back to <c>sys_created_on</c>,
    /// and <c>ClosedDate</c> is <c>closed_at</c> — nothing else. <c>resolved_at</c> is deliberately
    /// not read: Resolved is a Doing state, so a record that never reached Closed has not finished.
    /// The resulting span is request-to-closure, not time-in-progress.
    /// </para>
    /// </remarks>
    public static class ServiceNowWorkItemMapper
    {
        /// <summary>The human-readable record number, e.g. <c>INC0010029</c>.</summary>
        public const string RecordNumberField = "number";

        /// <summary>The record title.</summary>
        public const string TitleField = "short_description";

        /// <summary>The platform's own handle for the record, and the key history is fetched by.</summary>
        public const string RecordIdField = "sys_id";

        /// <summary>The record's own class, which is its kind of work (ADR-123 decision 8).</summary>
        public const string RecordClassField = "sys_class_name";

        /// <summary>A numeric choice value whose label is the thing a flow coach maps.</summary>
        public const string StateField = "state";

        public const string CreatedField = "sys_created_on";

        public const string OpenedField = "opened_at";

        public const string ClosedField = "closed_at";

        /// <summary>The universal-time half of a <c>sysparm_display_value=all</c> field.</summary>
        internal const string UniversalForm = "value";

        /// <summary>The instance-local, human-readable half of the same field.</summary>
        internal const string ReadableForm = "display_value";

        /// <summary>
        /// Reads the state label a flow coach recognises ("In Progress"), never the raw choice
        /// integer. US-02 AC3.
        /// </summary>
        public static string ReadStateLabel(JsonElement record)
        {
            return ReadForm(record, StateField, ReadableForm);
        }

        /// <summary>
        /// The number the service desk quotes, e.g. <c>INC0010029</c>. Not unique on a real instance
        /// and never an identity — see <see cref="ReadRecordId"/>.
        /// </summary>
        public static string ReadRecordNumber(JsonElement record)
        {
            return ReadForm(record, RecordNumberField, UniversalForm);
        }

        /// <summary>
        /// The record's <c>sys_id</c> — the handle <c>metric_instance.id</c> is keyed on, the only
        /// way to ask for a batch of records' history, and the only field that tells one record from
        /// another across pages. Distinct from <see cref="ReadRecordNumber"/>, which is what a
        /// service desk quotes, what Lighthouse shows, and what a busy instance re-issues.
        /// </summary>
        public static string ReadRecordId(JsonElement record)
        {
            return ReadForm(record, RecordIdField, UniversalForm);
        }

        /// <summary>
        /// Maps one ServiceNow record onto a Lighthouse work item. US-02 AC2.
        /// </summary>
        /// <param name="record">A record from a <c>sysparm_display_value=all</c> response.</param>
        /// <param name="owner">The team whose state mapping decides category and mapped name.</param>
        /// <param name="scope">What this team named its work, under both names (ADR-128). The
        /// record's class is reported back in the words THIS team used, so a team's config and its
        /// work items always agree — whichever form was typed. Also the fallback kind of work where
        /// the record does not say its own (ADR-123 decision 8).</param>
        public static WorkItemBase MapRecord(
            JsonElement record, IWorkItemQueryOwner owner, ServiceNowReadScope scope, string instanceUrl)
        {
            var stateLabel = ReadStateLabel(record);
            var recordNumber = ReadRecordNumber(record);
            var stateCategory = owner.MapStateToStateCategory(stateLabel);
            var recordClass = ReadForm(record, RecordClassField, UniversalForm);

            return new WorkItemBase
            {
                ReferenceId = recordNumber,
                Name = ReadForm(record, TitleField, UniversalForm),
                Type = KindOfWork(recordClass, scope),
                Url = RecordAddress(record, recordClass, instanceUrl),
                State = owner.MapRawStateToMappedName(stateLabel),
                StateCategory = stateCategory,
                Order = recordNumber,
                CreatedDate = ReadInstant(record, CreatedField),
                StartedDate = ReadInstant(record, OpenedField) ?? ReadInstant(record, CreatedField),
                ClosedDate = WhenWorkFinished(record, stateCategory),
            };
        }

        /// <summary>
        /// Where the record lives in ServiceNow, so a coach can open it from a Lighthouse chart the
        /// way they already can on every other connector.
        /// </summary>
        /// <remarks>
        /// Story #5612 item 1. Built from the record's OWN class rather than from the team's kinds of
        /// work: the <c>.do</c> path is class-specific, so a team reading incidents and change
        /// requests needs a different path per row. Both parts are already in the payload — nothing
        /// here costs a request.
        /// <para>
        /// No address at all when either part is missing. A link that 404s is worse than an absent
        /// one, and <c>WorkItemsDialog</c> already renders the id as plain text when the url is null.
        /// </para>
        /// </remarks>
        private static string? RecordAddress(JsonElement record, string recordClass, string instanceUrl)
        {
            var recordId = ReadRecordId(record);

            if (string.IsNullOrWhiteSpace(instanceUrl)
                || string.IsNullOrWhiteSpace(recordClass)
                || string.IsNullOrWhiteSpace(recordId))
            {
                return null;
            }

            // The same trim Jira's connector already does on its user-entered base url
            // (JiraWorkTrackingConnector.cs:1297); ServiceNow's Instance Url is just as user-entered.
            return $"{instanceUrl.TrimEnd('/')}/{recordClass}.do?sys_id={recordId}";
        }

        // ADR-123 decision 8. A record class IS a work item type, so a change request read through
        // the task hierarchy says change_request. The fallback is not padding: ReadForm answers
        // string.Empty for a field that is not there, and an empty Type on every row of a table that
        // does not carry sys_class_name would be a worse silent data change than the one being fixed.
        private static string KindOfWork(string recordClass, ServiceNowReadScope scope)
        {
            // ADR-128, amended: the words THIS TEAM used for that class, not a globally-chosen label.
            // A team configured with `change_request` stores change_request; one configured with
            // `Change Request` stores Change Request. Config and work items therefore agree by
            // CONSTRUCTION, for every team and every form, with nothing to normalise on save and no
            // path -- API, CLI or MCP -- that can route around it.
            //
            // sys_class_name.display_value arrives free on this very record and is still deliberately
            // not read: it answers what the INSTANCE calls the class, which is a third vocabulary, and
            // GetCreatedItemsForTeam compares Type against the team's own configured entries.
            // The fallback goes through AsTyped too. It is the same rule, not a separate one: a team
            // that typed `Task` must not have a row silently stored as `task` just because that row
            // declined to say its own class.
            return scope.AsTyped(
                string.IsNullOrWhiteSpace(recordClass) ? ServiceNowReadScope.RootTable : recordClass);
        }

        /// <summary>
        /// Only finished work carries a finish date, the way every other connector already couples
        /// the two. ServiceNow's reopen path does not reliably clear a closure instant, so a reopened
        /// record can arrive with one and a state the team maps to Doing. Carrying both would hide it
        /// from every chart at once: Throughput counts Done only, and the WIP series drops anything
        /// closed on or before the day being drawn.
        /// </summary>
        private static DateTime? WhenWorkFinished(JsonElement record, StateCategories stateCategory)
        {
            if (stateCategory != StateCategories.Done)
            {
                return null;
            }

            return ReadInstant(record, ClosedField);
        }

        /// <summary>
        /// Instants always come from the universal form. The instance-local form of the same field
        /// can fall on a different calendar day, and Throughput buckets by day.
        /// </summary>
        /// <remarks>
        /// <c>AdjustToUniversal | AssumeUniversal</c> rather than a trailing <c>SpecifyKind</c>: a
        /// value carrying <c>Z</c> or an offset is converted to universal time instead of being
        /// relabelled with the host machine's own wall clock, and a value carrying neither is read
        /// as universal rather than as local. Bug #5567 is the ledger entry for what relabelling
        /// costs.
        /// </remarks>
        internal static DateTime? ReadInstant(JsonElement record, string field)
        {
            const DateTimeStyles asUniversalTime = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;

            var universalTime = ReadForm(record, field, UniversalForm);

            if (!DateTime.TryParse(universalTime, CultureInfo.InvariantCulture, asUniversalTime, out var instant))
            {
                return null;
            }

            return instant;
        }

        // A field can be absent, an explicit JSON null (measured: the shape an unset date takes on a
        // class that declares the column but never fills it), a bare scalar rather than the two-form
        // object, or a number where a string was expected. GetString() throws on anything that is
        // neither string nor null, and that exception would take the whole team sync down with a
        // stack trace no rung explains.
        internal static string ReadForm(JsonElement record, string field, string form)
        {
            if (record.ValueKind != JsonValueKind.Object || !record.TryGetProperty(field, out var bothForms))
            {
                return string.Empty;
            }

            if (bothForms.ValueKind != JsonValueKind.Object || !bothForms.TryGetProperty(form, out var value))
            {
                return string.Empty;
            }

            return value.ValueKind switch
            {
                // Stryker disable once String: GetString() cannot return null for a String kind, so
                // the coalesce is here for the compiler and no input can reach it.
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
                _ => value.ToString(),
            };
        }
    }
}
