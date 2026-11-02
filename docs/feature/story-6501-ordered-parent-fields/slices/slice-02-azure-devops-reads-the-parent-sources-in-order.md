# Slice 02: Azure DevOps reads the parent sources in order (US-02, ADO #6501)

**Hypothesis:** the list built in slice 01 is connector-neutral; Azure DevOps only needs its own read of the entries.
Disproved if the ADO connector's relations optimisation (skip relations when the parent comes from a field) cannot be
restated for a list without extra requests on installs that use one source.

**Scope**
- `GetParentReference` and the relations read use the list; "Parent (Azure DevOps' own)" is optional and movable, read
  only when listed; the slice 01 migration applies to ADO owners.
- A relation type listed as a source is reported in Refresh History and skipped (unchanged from a single override).
- ADO readings are field values and the tracker's own parent only, so ADO records never need stored Features and the
  refresh's second pass does not run for them. The relations read (`GetRelationsOfWorkItems`) is skipped unless the list
  holds the own Parent or dependencies are read; a relation-type entry or a field that cannot be classified
  (`CouldNotTellWhatItNames`) no longer forces it, because the own Parent is now an explicit entry. The unclassifiable
  field is read as a field, as today.
- The connector's direct writes of the stored parent (`AzureDevOpsWorkTrackingConnector.cs:148` and `:694`) go through
  the one helper `ParentSourceSelector.HangUnder`, so the scan rule on parent assignments has no ADO exception.
- Single/none results pinned; request count pinned for the migrated list. What the migration does for an Azure DevOps
  field override (which today reads relations only for the dependency setup) follows UPGRADE-1 (A) in the design (DD-4):
  override owners are re-read once and read relations from then on (more requests, watch for 429 back-off); an item of
  such an owner with no override value and a native parent newly hangs under it.

**Out:** relation types as parent sources (a separate Story), connection default.

**Done when:** US-02 UAT passes (six scenarios).

**Size:** 1 day.

**Release order:** depends on slice 01 (model, migration, API, list control); released only after it. Dropping this
slice is safe: slice 01 stands alone.
