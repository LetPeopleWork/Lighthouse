namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// Which ServiceNow tables hold more than one kind of record. Pure (ADR-114).
    /// </summary>
    /// <remarks>
    /// ADR-123 decision 5: a static known-hierarchy set rather than a <c>sys_db_object</c> lookup,
    /// which answers 403 to the accounts this connector is built for. Its one caller is the
    /// connection-scope history question (ADR-123 decision 10); the team schema no longer asks.
    /// </remarks>
    public static class ServiceNowTableHierarchy
    {
        private static readonly string[] RootTables = ["task"];

        /// <summary>Whether reading this table unfiltered would return several kinds of work.</summary>
        public static bool HasDescendants(string table)
        {
            return RootTables.Contains(table, StringComparer.Ordinal);
        }
    }
}
