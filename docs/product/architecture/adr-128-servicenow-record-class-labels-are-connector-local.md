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
carries the words the team itself used. Nothing outside the connector knows a mapping happened.**

1. `ServiceNowClassLabels` — a new pure class beside `ServiceNowReadScope` — holds the map for the
   stock `task` hierarchy and exposes `ClassFor(label)`, which **returns its input unchanged when it
   is not in the map**. It recognises a canonical class name before a label, and matches labels
   case-insensitively. The map is stored class→label and inverted internally; only the label→class
   direction is public, because after the amendment below nothing reads the other one.
2. **Outbound (config → query)**: `ServiceNowReadScope.For` maps each configured entry through
   `ClassFor` before building `sys_class_nameIN…`, so a team configured with `Change Request` reads
   change requests. `For` is the only construction point, so every downstream consumer —
   `ScopedQuery`, `BaselineQuery`, `DefinitionTables` and the per-class readability probe — receives
   record classes without knowing a translation happened.
3. **Inbound (record → Lighthouse)**: `ServiceNowWorkItemMapper.KindOfWork` returns
   `scope.AsTyped(sys_class_name)` — **the words this team used**, not a globally-chosen label. A
   team configured with `change_request` stores `change_request`; one configured with
   `Change Request` stores `Change Request`. The fallback for a record that declares no class goes
   through `AsTyped` too — it is the same rule, so the invariant has no hole in it.
4. **Messages**: the readability probes ask about the record class; every refusal names the typed
   form, so a coach who typed `Change Request` is never answered about `change_request`.
5. `sys_class_name.display_value` is **not** read, even though it is free.

### Amendment, 2026-08-01 — point 3 replaces a normalisation step

This ADR first said `KindOfWork` returns `LabelFor(...)` (a global label) and added a fifth decision:
*on save, a team's `WorkItemTypes` are normalised to the label form*. Both are withdrawn in favour of
`AsTyped`, which makes config and data agree **by construction** instead of by a save-time step.

The prompt was a question during DELIVER — *if existing teams are disposable, is the divergence still
a problem?* It is: class names remain a legal input by design, and #5610's board picker actively
produces them, so a brand-new team hits it. That ruled out solving it by deleting data and forced a
better answer. Three reasons it is better:

- **The normalisation had no home.** `SyncTeamWithTeamSettings` is connector-agnostic and has no
  work-tracking system type at hand, so the step could only land by widening a shared save path or by
  putting a write inside `ValidateTeamSettings`.
- **It could be bypassed.** A settings-save hook is routed around by the API, the CLI and the MCP
  server — the hole #5611 named when it observed that `isWorkItemTypesRequired` is *"a hint to the web
  UI, and PUT /api/teams/{id} also serves the CLI and the MCP server"*. Mapping at sync time has no
  such path.
- **It stopped mutating what the coach typed**, a cost this ADR had merely accepted.

**Consequence.** Two teams reading the same class store different `Type` strings if they were
configured differently. Invisible per team — work items, charts and rules are all team-scoped — and
visible only in `SuggestionsController.GetWorkItemTypesForTeams`, which aggregates across teams and
lists both forms. Accepted.

**It also weakens the display win**, which is worth saying plainly: a team configured with class names
still reads `change_request` in its Type column. Labels come from typing a label, or from the picker
pre-filling one — which makes OC-4's answer (*the picker should pre-fill the label*) load-bearing
rather than cosmetic.

## Consequences

**Config and data cannot diverge.** Whatever a team typed is what its work items say, so
`SuggestionsController` offers it and the Created Items run chart compares like with like. There is no
second field, no DTO change, no frontend change, no migration, and no new concept for the four
connectors that never had this problem — and, after the amendment, nothing to normalise either.

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

**Only one direction is public, and that is a review outcome.** The class→label accessor shipped
with the map and became unreachable the moment the amendment above replaced `KindOfWork`'s
`LabelFor` call with `AsTyped`. It was removed rather than kept "for when the picker needs it": a
public method whose only callers are its own tests earns nothing and flatters the mutation score.
The map data still holds both directions, so OC-4 can restore the accessor in a few lines.

**The map is a maintenance surface.** It is a static set in source for the same measured reason
ADR-116 decision 4 gives: `sys_db_object` carries class labels and is unreadable below `itil`, and
`sys_dictionary` is `200`/EMPTY at every rung. A runtime lookup would work for the maintainer and
return nothing for the customer. Being wrong about an entry costs nothing — an absent alias just means
the class name flows through as it does today.

~~**Normalisation on save mutates what the coach typed.**~~ **Withdrawn by the amendment above** —
nothing is normalised, so nothing is mutated. What a coach typed is what they get back, and what their
work items say.

**#5610's board picker must pre-fill the label**, and after the amendment this matters more than it
did. It currently pre-fills a class name; nothing normalises that afterwards, so such a team reads
`change_request` in its Type column forever. The picker is the main route by which a coach acquires
the label without typing it. Carried as OC-4 against #5610.

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| Read `sys_class_name.display_value` into `Type` | Desynchronises config from data for any class not in a map — the exact silent-zero the epic exists to prevent. |
| Normalise `WorkItemTypes` to one form on save | Was this ADR's own first answer. No home in connector-agnostic save code, bypassable by the API / CLI / MCP, and it mutated what the coach typed. See the amendment. |
| Separate `TypeLabel` field | EF column, two migrations, `Update()` copy line, and a second vocabulary imposed on four connectors that do not need it. |
| Frontend-only label formatting | The map would then exist on both stacks while the input direction stays in the backend — the twin-drift shape Bug #5613 already cost a release. |
| Runtime lookup against `sys_db_object` | Measured unreadable below `itil`; works for the maintainer, 403 for the customer. ADR-116 decision 4 already took this exit. |
| Leave it as class names, document it | 5611 already documented "class names not labels". The dogfood shows documentation does not fix a field a coach cannot fill. |
