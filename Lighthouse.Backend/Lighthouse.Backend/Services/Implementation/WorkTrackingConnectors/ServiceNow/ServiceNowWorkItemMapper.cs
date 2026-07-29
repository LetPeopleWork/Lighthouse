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

        // SCAFFOLD (DISTILL slice 02, Story #5575)
        private const string ScaffoldSentinel = "__scaffold__";

        /// <summary>
        /// Reads the state label a flow coach recognises ("In Progress"), never the raw choice
        /// integer. US-02 AC3.
        /// </summary>
        public static string ReadStateLabel(JsonElement record)
        {
            // SCAFFOLD (DISTILL slice 02, Story #5575)
            _ = record;
            return ScaffoldSentinel;
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
            // SCAFFOLD (DISTILL slice 02, Story #5575)
            // Sentinels rather than a throw: the failure then lands at the assertion site, and the
            // expected/actual diff reads as the specification. See distill/red-classification-slice-02.md.
            _ = record;
            _ = owner;
            _ = table;

            return new WorkItemBase
            {
                ReferenceId = ScaffoldSentinel,
                Name = ScaffoldSentinel,
                Type = ScaffoldSentinel,
                State = ScaffoldSentinel,
                StateCategory = StateCategories.Unknown,
                Order = ScaffoldSentinel,
                CreatedDate = DateTime.UnixEpoch,
                StartedDate = DateTime.UnixEpoch,
                ClosedDate = DateTime.UnixEpoch,
            };
        }
    }
}
