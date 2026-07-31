namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// Which ServiceNow tables hold more than one kind of record. Pure (ADR-114).
    /// </summary>
    /// <remarks>
    /// ADR-123 decision 5: a static known-hierarchy set rather than a <c>sys_db_object</c> lookup.
    /// That table answers 403 to the accounts this connector is built for, so a runtime answer would
    /// flip a settings field's visibility with the customer's credentials, and it cannot back a
    /// schema DTO that has to return the same thing to every caller.
    /// </remarks>
    public static class ServiceNowTableHierarchy
    {
        /// <summary>
        /// Twinned in <c>DataRetrievalSchemaDefaults.ts</c>. <c>serviceNowSchemaTwin.enforcement.test.ts</c>
        /// compares the two as sets, so drift in either direction fails (Bug #5613).
        /// </summary>
        public static readonly IReadOnlyList<string> RootTables = ["task"];

        /// <summary>Whether reading this table unfiltered would return several kinds of work.</summary>
        public static bool HasDescendants(string table)
        {
            return RootTables.Contains(table, StringComparer.Ordinal);
        }
    }
}
