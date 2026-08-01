namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// What one team is reading: the kinds of work it named, and the queries that scope a read to
    /// them. Pure (ADR-114).
    /// </summary>
    /// <remarks>
    /// ADR-123 decisions 1, 8 and 9 — one string used to answer the URL path, the work item type and
    /// the metric-definition scope at once, split apart here.
    /// <para>
    /// Everything below <see cref="NamesNoKindsOfWork"/> assumes the team named at least one kind of
    /// work. Both call sites refuse before asking (ADR-123 decision 6, amended 2026-07-31).
    /// </para>
    /// </remarks>
    public sealed class ServiceNowReadScope
    {
        /// <summary>
        /// The table every ServiceNow read is addressed to (ADR-116 decision 1, withdrawn
        /// 2026-07-31). <c>task</c> is ServiceNow's base work table and everything the ITSM
        /// applications file extends it, so rooting here and naming the classes is the one scope
        /// that cannot silently read less work than the team named.
        /// </summary>
        public const string RootTable = "task";

        private const string ConditionSeparator = "^";

        private readonly List<NamedKindOfWork> kindsOfWork;

        private ServiceNowReadScope(List<NamedKindOfWork> kindsOfWork)
        {
            this.kindsOfWork = kindsOfWork;
        }

        /// <summary>The record classes the team named, in the order it named them.</summary>
        public IReadOnlyList<string> KindsOfWork => [.. kindsOfWork.Select(kind => kind.RecordClass)];

        /// <summary>
        /// The words the flow coach actually typed for a record class — <c>Change Request</c> where
        /// <see cref="KindsOfWork"/> holds <c>change_request</c> (ADR-128).
        /// </summary>
        /// <remarks>
        /// Every query is built from the record class; every message is built from this. Without it a
        /// coach who typed a label is refused in words they never used, which is the platform
        /// vocabulary this whole story exists to stop leaking. Falls back to the class for anything
        /// this scope was not built from.
        /// </remarks>
        public string AsTyped(string recordClass)
        {
            var named = kindsOfWork.Find(kind => string.Equals(kind.RecordClass, recordClass, StringComparison.Ordinal));

            return named is null ? recordClass : named.AsTyped;
        }

        /// <summary>
        /// A team that named no kinds of work. It reads nothing and is refused at save time
        /// (ADR-123 decision 4).
        /// </summary>
        public bool NamesNoKindsOfWork => kindsOfWork.Count < 1;

        /// <summary>
        /// The one construction point, which is what lets ADR-128's translation happen once: every
        /// query path downstream — <see cref="ScopedQuery"/>, <see cref="BaselineQuery"/>,
        /// <see cref="DefinitionTables"/> and the per-class readability probe — reads
        /// <see cref="KindsOfWork"/> and therefore receives record classes, whichever form the coach
        /// typed.
        /// </summary>
        public static ServiceNowReadScope For(List<string> workItemTypes)
        {
            var named = workItemTypes
                .Where(kindOfWork => !string.IsNullOrWhiteSpace(kindOfWork))
                .Select(kindOfWork => kindOfWork.Trim())
                .Select(kindOfWork => new NamedKindOfWork(kindOfWork, ServiceNowClassLabels.ClassFor(kindOfWork)))
                .ToList();

            return new ServiceNowReadScope(named);
        }

        /// <summary>One kind of work under both its names: what the coach typed, and what the Table API filters on.</summary>
        private sealed record NamedKindOfWork(string AsTyped, string RecordClass);

        /// <summary>
        /// The team's own query, narrowed to the kinds of work it named. The clause is prepended,
        /// ahead of the team's query and of the ORDERBY the connector appends unconditionally —
        /// the order the SPIKE measured (ADR-123 decision 2).
        /// </summary>
        public string ScopedQuery(string teamsOwnQuery)
        {
            return $"{ClassClause()}{ConditionSeparator}{teamsOwnQuery}";
        }

        /// <summary>
        /// The denominator the widening detector measures itself against (ADR-124 decision 3): the
        /// same kinds of work, without the team's own query.
        /// </summary>
        public string BaselineQuery()
        {
            return ClassClause();
        }

        /// <summary>
        /// The tables state-span definitions can sit on. Definitions attach to concrete classes and
        /// never to a base table, so they are looked for on the kinds of work the team named rather
        /// than on the table it is rooted at (ADR-123 decision 9).
        /// </summary>
        public List<string> DefinitionTables()
        {
            return [.. KindsOfWork];
        }

        private string ClassClause()
        {
            return Matching(ServiceNowWorkItemMapper.RecordClassField, [.. KindsOfWork]);
        }

        /// <summary>
        /// One <c>IN</c> condition for several values, an equality for exactly one
        /// (ADR-123 decision 2). <c>IN</c> is one condition against the 8192-byte URL cliff instead
        /// of 2n−1, and <c>=</c> is the only single-value form on record.
        /// </summary>
        internal static string Matching(string field, List<string> values)
        {
            return values.Count == 1
                ? $"{field}={values[0]}"
                : $"{field}IN{string.Join(",", values)}";
        }
    }
}
