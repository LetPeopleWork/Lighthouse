# Slice 03: The connection holds the parent sources as the default (US-03, ADO #6501)

**Hypothesis:** installs with many Teams on one connection would rather set the order once. Disproved if the reporter has
one Team; then drop this slice, nothing else depends on it.

**Scope**
- The connection's Teams default and Portfolios default each hold an ordered list under the same rules (one to five
  entries, no duplicate); same list control.
- Owners that follow the default read and display it; an existing single default shows as `[default, own Parent]`, an
  unset one as `[own Parent]`, and followers keep their parents.
- The connection save refuses removing a field a default still names. The connection's legacy integer defaults are
  written through for one release (same projection and rollback limit as the owners); a default migrated from a single
  default behaves as UPGRADE-1 (design DD-4) settles for owners. The two registry rows in `ConnectionFieldDefaults.cs`
  that read and write the legacy integers are migrated to the list-valued default here, and their allow-list entry in
  the legacy-column scan rule is removed in the same slice.
- Follower count and refresh queueing as today (ADR-242).

**Out:** moving the connection page (Epic 6473), per-level defaults (Epic 6474).

**Done when:** US-03 UAT passes (five scenarios).

**Size:** 1 day. Droppable alone.

**Release order:** depends on slice 01; released only after it. Dropping it is safe.
