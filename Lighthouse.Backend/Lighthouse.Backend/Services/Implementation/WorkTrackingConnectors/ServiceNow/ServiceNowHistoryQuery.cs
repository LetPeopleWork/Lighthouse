using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // ADR-118 decisions 2 and 4. What to ask metric_instance for, and what to keep of the answer.
    // Pure: the connector still owns the HTTP, so none of this needs an HttpMessageHandler to test.
    public static class ServiceNowHistoryQuery
    {
        /// <summary>
        /// How many record ids ride in one `idIN` list.
        /// </summary>
        /// <remarks>
        /// Measured: the encoded query rides in the URL, and the cliff is the 8192-byte limit —
        /// 245 ids answered 200 at 8182 bytes, 250 answered 414 at 8347. 200 leaves ~18 % headroom
        /// for the other sysparm_* parameters and for customer instances on longer hostnames or a
        /// reverse-proxy subpath than the PDI this was measured on.
        /// </remarks>
        public const int RecordsPerBatch = 200;

        /// <summary>Only this kind of definition measures how long a field held each of its values.</summary>
        /// <remarks>
        /// The STORED value, which is what <c>sysparm_query</c> matches on. The label a customer sees
        /// in the UI is "Field value duration", and asking for that form answered 200 with an empty
        /// result on a stock PDI whose definitions are all present and active.
        /// </remarks>
        public const string StateSpanDefinitionType = "field_value_duration";

        private const string DefinitionTableField = "table";

        /// <summary>The record class a definition is attached to. Never a base table (ADR-123 D9).</summary>
        public static string ReadDefinitionTable(JsonElement definition)
        {
            return ServiceNowWorkItemMapper.ReadForm(
                definition, DefinitionTableField, ServiceNowWorkItemMapper.UniversalForm);
        }

        private const string DefinitionTypeField = "type";

        private const string SpanRecordField = "id";

        private const string SpanDefinitionField = "definition";

        /// <summary>The state label. Never <c>field_value</c>, which is the choice number (ADR-118 D3).</summary>
        private const string SpanLabelField = "value";

        private const string SpanStartField = "start";

        /// <summary>
        /// Splits a team's records into batches small enough that the encoded query stays under the
        /// instance's URL limit. Going over is a 414, which is loud — but it is still a failed sync.
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<string>> IntoBatches(IReadOnlyList<string> recordIds)
        {
            return [.. recordIds.Chunk(RecordsPerBatch)];
        }

        /// <summary>
        /// The query that finds the definitions measuring state spans on the tables a team's work
        /// can sit on.
        /// </summary>
        /// <remarks>
        /// ADR-123 decision 9. Definitions attach to concrete record classes and never to a base
        /// table — measured 0 for <c>table=task</c> against 6 for
        /// <c>tableINincident,change_request</c> — so a team covering several kinds of work looks on
        /// each of them. Same two-form rule as the class clause, for the same reason.
        /// </remarks>
        public static string DefinitionQueryFor(List<string> tables)
        {
            return $"{ServiceNowReadScope.Matching(DefinitionTableField, tables)}^{DefinitionTypeField}={StateSpanDefinitionType}";
        }

        /// <summary>
        /// The query that fetches one batch of records' spans, restricted to the definitions that
        /// actually measure state.
        /// </summary>
        /// <remarks>
        /// Unencoded on purpose: the caller escapes it when it builds the URI, and escaping there
        /// twice over would cost the budget a full batch already fills.
        /// </remarks>
        public static string SpanQueryFor(IReadOnlyList<string> recordIds, IReadOnlyList<string> definitionIds)
        {
            return $"{SpanRecordField}IN{string.Join(",", recordIds)}" +
                $"^{SpanDefinitionField}IN{string.Join(",", definitionIds)}";
        }

        /// <summary>
        /// Turns metric_instance rows into spans, keeping only those produced by a definition that
        /// measures state.
        /// </summary>
        /// <remarks>
        /// The definition filter is the whole point (ADR-118 D2). The same `field` carries rows from
        /// script-calculation definitions — "Create to Resolve Duration", "First Call Resolution" —
        /// which are not spans at all. Keeping them would invent transitions out of things that are
        /// not state changes.
        /// </remarks>
        public static IReadOnlyList<ServiceNowStateSpan> SpansFrom(
            IReadOnlyList<JsonElement> rows, IReadOnlyCollection<string> stateSpanDefinitionIds)
        {
            return [.. rows.Select(row => SpanFrom(row, stateSpanDefinitionIds)).OfType<ServiceNowStateSpan>()];
        }

        private static ServiceNowStateSpan? SpanFrom(JsonElement row, IReadOnlyCollection<string> stateSpanDefinitionIds)
        {
            var definitionId = ServiceNowWorkItemMapper.ReadForm(
                row, SpanDefinitionField, ServiceNowWorkItemMapper.UniversalForm);

            if (!stateSpanDefinitionIds.Contains(definitionId))
            {
                return null;
            }

            // A row whose start cannot be read is not a span. Substituting any instant — epoch, now,
            // the record's creation — puts a fabricated moment into a chart that reads as measurement.
            var start = ServiceNowWorkItemMapper.ReadInstant(row, SpanStartField);

            if (start == null)
            {
                return null;
            }

            return new ServiceNowStateSpan(
                ServiceNowWorkItemMapper.ReadForm(row, SpanRecordField, ServiceNowWorkItemMapper.UniversalForm),
                ServiceNowWorkItemMapper.ReadForm(row, SpanLabelField, ServiceNowWorkItemMapper.ReadableForm),
                start.Value);
        }
    }
}
