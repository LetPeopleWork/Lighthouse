namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// What one team is reading: the table its connection is rooted at, and the kinds of work it
    /// named. Pure (ADR-114).
    /// </summary>
    /// <remarks>
    /// ADR-123 decision 1. One string used to answer four questions at once — the URL path, the work
    /// item type, the metric-definition scope and the subject of every validation message — and
    /// decisions 8 and 9 split three of them apart. This is also the single point slice 02's per-team
    /// table plugs into.
    /// </remarks>
    public sealed class ServiceNowReadScope
    {
        private const string ConditionSeparator = "^";

        private readonly List<string> kindsOfWork;

        private ServiceNowReadScope(string table, List<string> kindsOfWork)
        {
            Table = table;
            this.kindsOfWork = kindsOfWork;
        }

        /// <summary>The table every read of this team's work is addressed to.</summary>
        public string Table { get; }

        /// <summary>The record classes the team named, in the order it named them.</summary>
        public IReadOnlyList<string> KindsOfWork => kindsOfWork;

        /// <summary>
        /// A team whose table has descendants and that named no kinds of work. Reading it would
        /// return the whole hierarchy, so it reads nothing and is refused at save time
        /// (ADR-123 decision 4).
        /// </summary>
        public bool ReadsAWholeHierarchy => NamesNoKindsOfWork && ServiceNowTableHierarchy.HasDescendants(Table);

        private bool NamesNoKindsOfWork => kindsOfWork.Count < 1;

        public static ServiceNowReadScope For(string table, List<string> workItemTypes)
        {
            var named = workItemTypes
                .Where(kindOfWork => !string.IsNullOrWhiteSpace(kindOfWork))
                .Select(kindOfWork => kindOfWork.Trim())
                .ToList();

            return new ServiceNowReadScope(table, named);
        }

        /// <summary>
        /// The team's own query, narrowed to the kinds of work it named. The clause is prepended,
        /// ahead of the team's query and of the ORDERBY the connector appends unconditionally —
        /// the order the SPIKE measured (ADR-123 decision 2).
        /// </summary>
        public string ScopedQuery(string teamsOwnQuery)
        {
            return NamesNoKindsOfWork
                ? teamsOwnQuery
                : $"{ClassClause()}{ConditionSeparator}{teamsOwnQuery}";
        }

        /// <summary>
        /// The denominator the widening detector measures itself against (ADR-124 decision 3): the
        /// same kinds of work, without the team's own query. Null where none were named, which is
        /// the unfiltered count every shipped team already compares against.
        /// </summary>
        public string? BaselineQuery()
        {
            return NamesNoKindsOfWork ? null : ClassClause();
        }

        /// <summary>
        /// The tables state-span definitions can sit on. Definitions attach to concrete classes and
        /// never to a base table, so a team that named kinds of work looks on those and a team that
        /// named none looks where it always did (ADR-123 decision 9).
        /// </summary>
        public List<string> DefinitionTables()
        {
            return NamesNoKindsOfWork ? [Table] : [.. kindsOfWork];
        }

        private string ClassClause()
        {
            return Matching(ServiceNowWorkItemMapper.RecordClassField, kindsOfWork);
        }

        /// <summary>
        /// One <c>IN</c> condition for several values, an equality for exactly one
        /// (ADR-123 decision 2). <c>IN</c> is one condition against the 8192-byte URL cliff instead
        /// of 2n−1, and <c>=</c> is the only single-value form on record — which is what keeps every
        /// shipped leaf-rooted read byte-identical.
        /// </summary>
        internal static string Matching(string field, List<string> values)
        {
            return values.Count == 1
                ? $"{field}={values[0]}"
                : $"{field}IN{string.Join(",", values)}";
        }
    }
}
