namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// What each kind of work is called, in both directions: the record class the Table API filters
    /// on, and the label a flow coach reads on their own ServiceNow screen. Pure (ADR-114).
    /// </summary>
    /// <remarks>
    /// ADR-128. A static set in source for the same measured reason ADR-116 decision 4 gives:
    /// <c>sys_db_object</c> carries class labels and is unreadable below <c>itil</c>, and
    /// <c>sys_dictionary</c> is 200/EMPTY at every rung, so a runtime lookup would work for the
    /// maintainer and return nothing for the customer.
    /// <para>
    /// <b>Passthrough is the load-bearing behaviour, not the edge case.</b> A name that is not in
    /// the map comes back unchanged in BOTH directions, so a shop's own <c>u_maintenance_task</c>
    /// stores as <c>u_maintenance_task</c> and its team config stays <c>u_maintenance_task</c> —
    /// unimproved, but consistent, and therefore still correct in every comparison. This is why
    /// <c>sys_class_name.display_value</c> is deliberately not read even though it arrives free on
    /// every row: it would give that class a pretty label on the item while its config entry kept
    /// the class name, and <c>GetCreatedItemsForTeam</c> compares the two.
    /// </para>
    /// </remarks>
    public static class ServiceNowClassLabels
    {
        // __SCAFFOLD__ — DISTILL (ADR-128). DELIVER replaces the bodies; the signatures are the
        // contract the acceptance tests are written against.

        /// <summary>
        /// The label for a record class — <c>change_request</c> becomes <c>Change Request</c>. A
        /// class with no entry is returned unchanged.
        /// </summary>
        public static string LabelFor(string recordClass)
        {
            throw new NotSupportedException("__SCAFFOLD__ ServiceNowClassLabels.LabelFor is not yet implemented");
        }

        /// <summary>
        /// The record class for a label — <c>Change Request</c> becomes <c>change_request</c>.
        /// Case-insensitive. A name with no entry is returned unchanged, which is how a class name
        /// typed directly, and a custom class, both keep working.
        /// </summary>
        public static string ClassFor(string kindOfWork)
        {
            throw new NotSupportedException("__SCAFFOLD__ ServiceNowClassLabels.ClassFor is not yet implemented");
        }
    }
}
