# ADR-128 — ServiceNow record-class labels are a connector-local, bidirectional map

**Status**: Accepted
**Date**: 2026-08-01
**Feature**: `servicenow-pre-close-bucket` (ADO [#5612](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5612))
**Amends**: ADR-123 decision 8 (a record class *is* a work item type)
**Related**: ADR-116 decision 4 (static set in source, because `sys_db_object` is unreadable)

## Context

ADR-123 decision 8 made a record's own class its work item type, so a ServiceNow team shows
`change_request` where a Jira team shows `Bug`. Two consequences surfaced while dogfooding:

1. A coach reads `change_request` in the Type column, in every chart legend, and in the marker colour
   key. Every other connector supplies a human label there, because in Jira, ADO and Linear the type
   *is* a label.
2. A coach must **type** `change_request` into a team's Work Item Types. A wrong entry narrows
   `sys_class_nameIN…` to zero rows silently (5611 SPIKE) — the failure mode this epic exists to
   prevent — and the value is a column name nobody sees in ServiceNow's own UI.

The obvious repairs both fail:

- **Read `sys_class_name.display_value`** (it is free, present on every row, and gives "Catalog Task"
  for `sc_task`). But then a class Lighthouse does not recognise stores its *instance label* while the
  team's config holds its *class name*, and the two no longer match — `TeamMetricsService`'s Created
  Items run chart compares them and silently returns zero.
- **Carry a second field** (`Type` stays the key, a new `TypeLabel` carries the label). This adds an EF
  column, two migrations, a line in the hand-written `WorkItemBase.Update()` copy, and a second
  vocabulary that every consumer must learn to choose between. The cost lands on four connectors that
  have no such problem.

## Decision

**The ServiceNow connector owns a hardcoded bidirectional class↔label map, applies it in both
directions at its own boundary, and passes through anything it does not know. `WorkItemBase.Type`
carries the label. Nothing outside the connector knows a mapping happened.**

1. `ServiceNowRecordClasses` — a new pure class beside `ServiceNowReadScope` — holds the map for the
   stock ITSM task hierarchy and exposes `LabelFor(class)` and `ClassFor(label)`. Both are
   case-insensitive and both **return their input unchanged when it is not in the map**.
2. **Inbound (record → Lighthouse)**: `ServiceNowWorkItemMapper.KindOfWork` returns
   `LabelFor(sys_class_name)`, so `Type` is `Change Request`.
3. **Outbound (config → query)**: `ServiceNowReadScope.For` maps each configured entry through
   `ClassFor` before building `sys_class_nameIN…`, so a team configured with `Change Request` reads
   change requests.
4. **On save**: a ServiceNow team's `WorkItemTypes` are normalised to the label form, so a coach who
   typed `change_request` and a coach who typed `Change Request` end up with the same stored value.
5. `sys_class_name.display_value` is **not** read, even though it is free.

## Consequences

**The two vocabularies converge instead of coexisting.** Config holds labels, `Type` holds labels,
`SuggestionsController` offers labels, and the Created Items run chart compares label to label. There
is no second field, no DTO change, no frontend change, no migration, and no new concept for the four
connectors that never had this problem.

**Passthrough keeps unknown classes coherent.** A shop's custom `u_maintenance_task` is not in the
map, so it stores as `u_maintenance_task` and its config entry stays `u_maintenance_task` — unimproved,
but *consistent*, and therefore still correct in every comparison. This is precisely why point 5
rejects `display_value`: reading it would give the custom class a pretty label on the item while its
config entry kept the class name, and the two would stop matching. A free field that desynchronises is
worse than no field.

**A renamed stock class shows Lighthouse's label, not the instance's.** An instance that calls
`change_request` "RFC" still reads *Change Request*. Accepted: the map is a convenience over a value
that was previously shown raw, so the worst case is a different English label rather than a wrong
number. Revisit only if a customer reports it.

**The map is a maintenance surface.** It is a static set in source for the same measured reason
ADR-116 decision 4 gives: `sys_db_object` carries class labels and is unreadable below `itil`, and
`sys_dictionary` is `200`/EMPTY at every rung. A runtime lookup would work for the maintainer and
return nothing for the customer. Being wrong about an entry costs nothing — an absent alias just means
the class name flows through as it does today.

**Normalisation on save mutates what the coach typed.** Typing `change_request` and getting
`Change Request` back is a visible change. Accepted, and arguably the point: it is the product
teaching its own vocabulary, and without it a class-name-typed config silently breaks the Created
Items forecast.

**#5610's board picker must pre-fill the label.** It currently pre-fills a class name into Work Item
Types. Under this ADR that value would be normalised on save anyway, but pre-filling the label is what
the coach should see in the field. Carried as OC-4 against #5610.

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| Read `sys_class_name.display_value` into `Type` | Desynchronises config from data for any class not in a map — the exact silent-zero the epic exists to prevent. |
| Separate `TypeLabel` field | EF column, two migrations, `Update()` copy line, and a second vocabulary imposed on four connectors that do not need it. |
| Frontend-only label formatting | The map would then exist on both stacks while the input direction stays in the backend — the twin-drift shape Bug #5613 already cost a release. |
| Runtime lookup against `sys_db_object` | Measured unreadable below `itil`; works for the maintainer, 403 for the customer. ADR-116 decision 4 already took this exit. |
| Leave it as class names, document it | 5611 already documented "class names not labels". The dogfood shows documentation does not fix a field a coach cannot fill. |
