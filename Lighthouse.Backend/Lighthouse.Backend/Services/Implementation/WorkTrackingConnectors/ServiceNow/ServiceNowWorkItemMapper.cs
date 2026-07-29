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
    /// <b>ADR-117.</b> <c>StartedDate</c> is <c>opened_at</c> falling back to <c>sys_created_on</c>;
    /// <c>ClosedDate</c> is <c>resolved_at</c> falling back to <c>closed_at</c>. <c>closed_at</c> is
    /// EMPTY on Resolved (state 6), so keying on it alone silently drops every resolved-but-not-closed
    /// record from Throughput. The resulting span is request-to-resolution, not time-in-progress.
    /// </para>
    /// </remarks>
    public static class ServiceNowWorkItemMapper
    {
        /// <summary>The human-readable record number, e.g. <c>INC0010029</c>.</summary>
        public const string RecordNumberField = "number";

        /// <summary>The record title.</summary>
        public const string TitleField = "short_description";

        /// <summary>A numeric choice value whose label is the thing a flow coach maps.</summary>
        public const string StateField = "state";

        public const string CreatedField = "sys_created_on";

        public const string OpenedField = "opened_at";

        public const string ResolvedField = "resolved_at";

        public const string ClosedField = "closed_at";

        /// <summary>The universal-time half of a <c>sysparm_display_value=all</c> field.</summary>
        private const string UniversalForm = "value";

        /// <summary>The instance-local, human-readable half of the same field.</summary>
        private const string ReadableForm = "display_value";

        /// <summary>
        /// Reads the state label a flow coach recognises ("In Progress"), never the raw choice
        /// integer. US-02 AC3.
        /// </summary>
        public static string ReadStateLabel(JsonElement record)
        {
            return ReadForm(record, StateField, ReadableForm);
        }

        /// <summary>
        /// The number the service desk quotes, e.g. <c>INC0010029</c>. It is also what tells one
        /// record from another across pages.
        /// </summary>
        public static string ReadRecordNumber(JsonElement record)
        {
            return ReadForm(record, RecordNumberField, UniversalForm);
        }

        /// <summary>
        /// Maps one ServiceNow record onto a Lighthouse work item. US-02 AC2.
        /// </summary>
        /// <param name="record">A record from a <c>sysparm_display_value=all</c> response.</param>
        /// <param name="owner">The team whose state mapping decides category and mapped name.</param>
        /// <param name="table">The configured table, which is also the work item type — ITSM records
        /// carry no separate type field, which is why the team scope does not ask for one.</param>
        public static WorkItemBase MapRecord(JsonElement record, IWorkItemQueryOwner owner, string table)
        {
            var stateLabel = ReadStateLabel(record);
            var recordNumber = ReadRecordNumber(record);
            var stateCategory = owner.MapStateToStateCategory(stateLabel);

            return new WorkItemBase
            {
                ReferenceId = recordNumber,
                Name = ReadForm(record, TitleField, UniversalForm),
                Type = table,
                State = owner.MapRawStateToMappedName(stateLabel),
                StateCategory = stateCategory,
                Order = recordNumber,
                CreatedDate = ReadInstant(record, CreatedField),
                StartedDate = ReadInstant(record, OpenedField) ?? ReadInstant(record, CreatedField),
                ClosedDate = WhenWorkFinished(record, stateCategory),
            };
        }

        /// <summary>
        /// Only finished work carries a finish date, the way every other connector already couples
        /// the two. ServiceNow's reopen path does not reliably clear <c>resolved_at</c>, so a
        /// reopened incident arrives with a resolution instant and a state the team maps to Doing.
        /// Carrying both would hide it from every chart at once: Throughput counts Done only, and
        /// the WIP series drops anything closed on or before the day being drawn.
        /// </summary>
        private static DateTime? WhenWorkFinished(JsonElement record, StateCategories stateCategory)
        {
            if (stateCategory != StateCategories.Done)
            {
                return null;
            }

            return ReadInstant(record, ResolvedField) ?? ReadInstant(record, ClosedField);
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
        private static DateTime? ReadInstant(JsonElement record, string field)
        {
            const DateTimeStyles asUniversalTime = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;

            var universalTime = ReadForm(record, field, UniversalForm);

            if (!DateTime.TryParse(universalTime, CultureInfo.InvariantCulture, asUniversalTime, out var instant))
            {
                return null;
            }

            return instant;
        }

        // A field can be absent, an explicit JSON null (which is the shape change_request.resolved_at
        // returns), a bare scalar rather than the two-form object, or a number where a string was
        // expected. GetString() throws on anything that is neither string nor null, and that
        // exception would take the whole team sync down with a stack trace no rung explains.
        private static string ReadForm(JsonElement record, string field, string form)
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
