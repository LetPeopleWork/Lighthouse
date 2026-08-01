using System.Collections.Frozen;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// What each kind of work is called: the label a flow coach reads on their own ServiceNow
    /// screen, resolved to the record class the Table API filters on. Pure (ADR-114).
    /// </summary>
    /// <remarks>
    /// ADR-128. A static set in source for the same measured reason ADR-116 decision 4 gives:
    /// <c>sys_db_object</c> carries class labels and is unreadable below <c>itil</c>, and
    /// <c>sys_dictionary</c> is 200/EMPTY at every rung, so a runtime lookup would work for the
    /// maintainer and return nothing for the customer.
    /// <para>
    /// <b>Passthrough is the load-bearing behaviour, not the edge case.</b> A name that is not in
    /// the map comes back unchanged, so a shop's own <c>u_maintenance_task</c>
    /// stores as <c>u_maintenance_task</c> and its team config stays <c>u_maintenance_task</c> —
    /// unimproved, but consistent, and therefore still correct in every comparison. This is why
    /// <c>sys_class_name.display_value</c> is deliberately not read even though it arrives free on
    /// every row: it would give that class a pretty label on the item while its config entry kept
    /// the class name, and <c>GetCreatedItemsForTeam</c> compares the two.
    /// </para>
    /// <para>
    /// The entries are every class under <c>task</c> on a stock instance, read from
    /// <c>sys_db_object</c> on the PDI <c>dev191338</c> on 2026-08-01. Two of them are the reason
    /// this is a map rather than a transform: <c>sc_task</c> is <em>Catalog Task</em> and
    /// <c>release_task</c> is <em>Feature Task</em>, neither of which any rewriting of the class
    /// name produces. Being wrong about an entry costs nothing — it simply passes through.
    /// </para>
    /// <para>
    /// The class-to-label direction is deliberately NOT exposed. Once the ADR-128 amendment made
    /// <see cref="ServiceNowReadScope.AsTyped"/> report a record in the words its own team used,
    /// nothing needed it. If #5610's board picker comes to pre-fill labels (OC-4), that is the
    /// moment to add it back.
    /// </para>
    /// </remarks>
    public static class ServiceNowClassLabels
    {
        private static readonly FrozenDictionary<string, string> LabelByClass =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["task"] = "Task",
                ["alm_transfer_order_line_subtask"] = "Transfer Order Line Subtask",
                ["alm_transfer_order_line_task"] = "Transfer Order Line Task",
                ["asset_reclamation_request"] = "Asset Reclamation Request",
                ["asset_task"] = "Asset Task",
                ["business_app_request"] = "Business Application Request",
                ["cert_follow_on_task"] = "Follow On Task",
                ["change_phase"] = "Change Phase",
                ["change_request"] = "Change Request",
                ["change_request_imac"] = "IMAC",
                ["change_task"] = "Change Task",
                ["chat_queue_entry"] = "Chat Queue Entry",
                ["cmdb_data_management_task"] = "CMDB Data Management Task",
                ["cmdb_multisource_recomp_task"] = "CMDB Multisource Recompute Task",
                ["gsw_task"] = "Guided Setup Task",
                ["help_guidance_task"] = "Guidance Task",
                ["incident"] = "Incident",
                ["incident_task"] = "Incident Task",
                ["kb_feedback_task"] = "Knowledge Feedback Task",
                ["kb_knowledge_base_request"] = "Request new Knowledge Base",
                ["kb_submission"] = "Kb Submission",
                ["orphan_ci_remediation"] = "Orphan CI Remediation",
                ["problem"] = "Problem",
                ["problem_task"] = "Problem Task",
                ["reclassification_task"] = "Reclassification Task",
                ["recommended_field_remediation"] = "Recommended Field Remediation",
                ["reconcile_duplicate_task"] = "Reconcile Duplicate Task",
                ["release_phase"] = "Release Phase",
                ["release_task"] = "Feature Task",
                ["required_field_remediation"] = "Required Field Remediation",
                ["sc_req_item"] = "Requested Item",
                ["sc_request"] = "Request",
                ["sc_task"] = "Catalog Task",
                ["scan_task"] = "Scan Task",
                ["sn_collab_request_dev_collab_task"] = "Developer Collaboration Task",
                ["sn_creatorstudio_child_task"] = "Request Subtask",
                ["sn_creatorstudio_new_application_admin_task"] = "New Application Admin Task",
                ["sn_creatorstudio_new_application_task"] = "New Application Task",
                ["sn_creatorstudio_task"] = "Request Task",
                ["sn_deploy_pipeline_deployment_request"] = "Deployment Request",
                ["sn_itam_ztr_fulfillment_req"] = "Zero Touch Refresh Fulfillment Request",
                ["sn_vsc_security_task"] = "Security tasks",
                ["stale_ci_remediation"] = "Stale CI Remediation",
                ["statemgmt_renew_lease_task"] = "Renew Lease Task",
                ["std_change_proposal"] = "Standard Change Proposal",
                ["sys_report_access_request"] = "Report Access Request",
                ["sysapproval_group"] = "Group approval",
                ["ticket"] = "Ticket",
                ["upgrade_history_task"] = "Upgrade History Task",
                ["vtb_task"] = "Private Task",
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // ToDictionary over the labels throws if two classes share one, which is the fail-fast this
        // map wants: an ambiguous label has no correct answer and a silent winner would be decided by
        // dictionary order. Asserted directly in ServiceNowClassLabelsTest so it fails at test time
        // rather than at first use.
        private static readonly FrozenDictionary<string, string> ClassByLabel = LabelByClass
            .ToDictionary(entry => entry.Value, entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        // Any case of a class name to the one case ServiceNow stores. Recognising the name is not
        // enough: the canonical KEY has to come back, or `Change_Request` flows through unchanged and
        // the divergence below happens. A dictionary rather than a set because a set can only answer
        // whether the name is known, not what its stored form is.
        private static readonly FrozenDictionary<string, string> CanonicalClassByAnyCase = LabelByClass
            .Keys
            .ToDictionary(recordClass => recordClass, recordClass => recordClass, StringComparer.OrdinalIgnoreCase)
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The record class for a label — <c>Change Request</c> becomes <c>change_request</c>.
        /// Case-insensitive. A name with no entry is returned unchanged, which is how a class name
        /// typed directly, and a custom class, both keep working.
        /// </summary>
        /// <remarks>
        /// A record class is recognised before a label (ADR-128 decision b), in ANY case, and is
        /// answered with the form ServiceNow stores. Measured on the PDI 2026-08-01: `sysparm_query`'s
        /// <c>IN</c> matches a value case-insensitively, so <c>sys_class_nameINChange_Request</c>
        /// returns the same rows as the lowercase form — and those rows say <c>change_request</c>. A
        /// team left holding <c>Change_Request</c> would therefore sync fine and then disagree with its
        /// own work items, which is the silent zero this whole design exists to prevent.
        /// <para>
        /// Four labels equal their own class name ignoring case (Task, Incident, Problem, Ticket) and
        /// both lookups agree on those; no label resolves to a different class than this step would
        /// (asserted in <c>ServiceNowClassLabelsTest</c>), which is what makes class-first safe.
        /// </para>
        /// </remarks>
        public static string ClassFor(string kindOfWork)
        {
            if (CanonicalClassByAnyCase.TryGetValue(kindOfWork, out var canonical))
            {
                return canonical;
            }

            return ClassByLabel.TryGetValue(kindOfWork, out var recordClass) ? recordClass : kindOfWork;
        }
    }
}
