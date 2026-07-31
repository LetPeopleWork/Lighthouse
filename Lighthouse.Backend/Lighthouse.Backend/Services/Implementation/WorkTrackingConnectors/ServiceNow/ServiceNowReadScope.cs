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

        private readonly List<string> kindsOfWork;

        private ServiceNowReadScope(List<string> kindsOfWork)
        {
            this.kindsOfWork = kindsOfWork;
        }

        /// <summary>The record classes the team named, in the order it named them.</summary>
        public IReadOnlyList<string> KindsOfWork => kindsOfWork;

        /// <summary>
        /// A team that named no kinds of work. It reads nothing and is refused at save time
        /// (ADR-123 decision 4).
        /// </summary>
        public bool NamesNoKindsOfWork => kindsOfWork.Count < 1;

        public static ServiceNowReadScope For(List<string> workItemTypes)
        {
            var named = workItemTypes
                .Where(kindOfWork => !string.IsNullOrWhiteSpace(kindOfWork))
                .Select(kindOfWork => kindOfWork.Trim())
                .ToList();

            return new ServiceNowReadScope(named);
        }

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
            return [.. kindsOfWork];
        }

        private string ClassClause()
        {
            return Matching(ServiceNowWorkItemMapper.RecordClassField, kindsOfWork);
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
