using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // SCAFFOLD (DISTILL slice 04, Story #5577)
    //
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
        public const string StateSpanDefinitionType = "Field value duration";

        private const string ScaffoldSentinel = "__scaffold__";

        /// <summary>
        /// Splits a team's records into batches small enough that the encoded query stays under the
        /// instance's URL limit. Going over is a 414, which is loud — but it is still a failed sync.
        /// </summary>
        public static IReadOnlyList<IReadOnlyList<string>> IntoBatches(IReadOnlyList<string> recordIds)
        {
            // One batch holding one id that belongs to nobody: wrong for a team that fits in a single
            // batch, wrong for one that does not, and wrong for a team with no work at all. Echoing
            // the input back would have made the fits-in-one-batch case pass before batching existed.
            return [[ScaffoldSentinel]];
        }

        /// <summary>
        /// The query that finds the definitions measuring state spans on the configured table.
        /// </summary>
        public static string DefinitionQueryFor(string table)
        {
            return ScaffoldSentinel;
        }

        /// <summary>
        /// The query that fetches one batch of records' spans, restricted to the definitions that
        /// actually measure state.
        /// </summary>
        public static string SpanQueryFor(IReadOnlyList<string> recordIds, IReadOnlyList<string> definitionIds)
        {
            // Deliberately past the URL budget. A short sentinel would satisfy the length guard, and
            // that guard is the one standing between a full batch and a 414 that fails the sync.
            return new string('x', 8000);
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
            // TWO spans, whatever came in. Returning one would let the definition filter's own test
            // pass while filtering nothing — it asserts a single span survives, and a scaffold that
            // always answers with one would be right by accident about the decision this slice turns on.
            return
            [
                new ServiceNowStateSpan(ScaffoldSentinel, ScaffoldSentinel, DateTime.UnixEpoch),
                new ServiceNowStateSpan(ScaffoldSentinel, ScaffoldSentinel, DateTime.UnixEpoch),
            ];
        }
    }
}
