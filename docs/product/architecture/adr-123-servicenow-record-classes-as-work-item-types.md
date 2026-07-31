# ADR-123: ServiceNow work item types are record classes — one class-filtered read, class-scoped history, a static hierarchy-root set

- **Status**: **Proposed** (2026-07-31, Story 5611 slice 01 DESIGN) — pending maintainer ratification.
- **Date**: 2026-07-31
- **Feature**: servicenow-multi-table-work-item-types (ADO Story 5611, parent Epic 5513)
- **Deciders**: Benjamin Huser-Berta (maintainer)
- **Amends**: [ADR-116](./adr-116-servicenow-table-at-connection-scope.md) decision 6 (the C-3 soft call).
  [ADR-118](./adr-118-servicenow-transition-history-from-metric-instance-spans.md) D2 (definition scope).

## Context

A ServiceNow team reads exactly one table. A shop whose team handles incidents *and* changes has to
create two Lighthouse teams, which splits one team's throughput into two half-teams and makes both
forecasts wrong. That was found in the maintainer's own slice-02 dogfood, 2026-07-29.

DISCUSS D2 proposed the model: `task` is ServiceNow's base table and `incident`, `change_request`,
`problem`, `sc_task` and the rest all extend it, so *one* read of `task` filtered by `sys_class_name`
returns exactly "incidents and changes" — one query, one paging walk, one repeat guard, one state
choice list. The alternative, N reads over N (table, query) pairs, multiplies all four by N.

The SPIKE (2026-07-31, `docs/feature/servicenow-multi-table-work-item-types/spike/findings.md`)
measured the model against a live PDI holding 725 records across 14 classes. Six measurements bind
this decision:

1. **The filter binds correctly, in both available forms.** Read as sets of `sys_id` against the
   reference answer (each class read from its own table with the same team query), `^OR`-chained and
   `IN` forms both returned **identical sets — zero extra, zero missing** — across four team queries,
   including one carrying its own `^OR` and one carrying the connector's unconditional
   `^ORDERBYsys_created_on`. ServiceNow grouped it as `(class OR class) AND (team query)`.
2. **Unfiltered, a hierarchy-rooted read is 3.6× too wide.** The same team reads **579** records of 13
   classes where it wanted 159 of 2, and **725** of 14 with no query at all.
3. **`sys_class_name` carries names, not labels**, and it already rides in the connector's
   `sysparm_display_value=all` read: `{"display_value": "Change Request", "value": "change_request"}`.
   `sysparm_query` matches the stored value.
4. **Exact match is not hierarchy-inclusive.** `sys_class_name=task` returns the **30** records whose
   own class is `task`, not the 725 in the hierarchy.
5. **A bogus class name narrows, never widens** — `sys_class_nameINincident,not_a_real_class` returned
   the 70 incidents. This is the opposite of a bogus *field* name, which
   [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md) and
   `ServiceNowTeamQueryVerdict` exist to catch.
6. **A `task`-rooted team finds zero metric definitions.** `table=task^type=field_value_duration`
   returns **0**; `table=incident` returns 4; `tableINincident,change_request` returns 6.
   `metric_instance` agrees — 196 rows for `incident`, 0 for `task`. Definitions attach to concrete
   classes; nothing is ever attached to the base table.

Measurement 6 is the one that changes the shape of the slice: shipping the class filter *without*
touching history would take away, for the exact configuration this feature recommends, the
time-in-state capability [ADR-118](./adr-118-servicenow-transition-history-from-metric-instance-spans.md)
shipped four days earlier.

Two further constraints come from outside the SPIKE:

- **`Team` has no option bag.** It implements `IWorkTrackingSystemOptionsOwner`, which is only a
  connection id plus its navigation property. A per-team *table* needs a new persisted column and a
  migration across every provider; the class filter reuses `Team.WorkItemTypes`, which is already
  persisted, already on the DTO, already rendered, and merely hidden for ServiceNow today.
- **The data-retrieval schema is duplicated across the stacks and only one copy is exhaustive**
  (Bug #5613, `docs/evolution/2026-07-30-bug-5613-schema-twin-drift.md`). `isWorkItemTypesRequired`
  both hides the field and blocks the save, and the two copies feed different screens: the create
  wizard reads the frontend `Record`, the settings page reads the backend DTO. A disagreement produces
  a team that can be created and never saved.

## Decision

### 1. `Team.WorkItemTypes` is the ServiceNow record-class list, filtered into the read as `sys_class_name`

A ServiceNow class name *is* a work item type, not an analogy for one. No new field, no migration.
The values are class **names** (`change_request`), never labels (`Change Request`) — measurement 3.

### 2. The generated clause is `IN` for two or more classes, `=` for exactly one

| classes | clause |
|---|---|
| 0 | *(no clause — see decision 4)* |
| 1 | `sys_class_name=incident` |
| ≥ 2 | `sys_class_nameINincident,change_request` |

The `^OR` chain is **never generated**, even though it measured correct. Three reasons, in order of
weight: `IN` is one condition instead of *2n−1* against the 8192-byte URL cliff
`ServiceNowHistoryQuery.RecordsPerBatch` already measured; its correctness does not rest on a grouping
rule observed on exactly one instance version; and it is shorter to read in a support log.

The single-class case emits `=` rather than a one-element `IN` for one reason only: `=` is the form
that was measured (measurements 4 and 5), a one-element `IN` was not, and this connector's standing
rule is that a probe whose job is to distrust the substrate must not itself rest on an unmeasured
form (`ServiceNowWorkTrackingConnector.cs:813`, the `sysparm_fields` note). It also keeps every
currently-shipped leaf-rooted read byte-identical, which is what makes AC-B5 a claim rather than a
hope. The branch is one line in a pure function and can be deleted the day a one-element `IN` is
measured by the standing integration guard.

**The clause is prepended**, ahead of the team's own query, and therefore ahead of the `^ORDERBY`
`InAStableOrder` appends unconditionally. Both orders measured identical; prepending is the one the
SPIKE recommends and the one whose result set is on record.

### 3. The clause is emitted whenever classes are named — not when the table is a hierarchy root

The two conditions are deliberately not the same test.

| configured table | classes | behaviour |
|---|---|---|
| leaf (`incident`), shipped default | none | no clause. **Byte-identical to today** (AC-B5, AC-B2) |
| hierarchy root (`task`) | named | clause emitted (AC-B1) |
| hierarchy root (`task`) | none | **reads nothing and says why** (AC-B3, decision 4) |
| leaf (`incident`) | `["incident"]` | clause emitted; redundant, correct, and honours what the coach typed |

Hierarchy-root knowledge is therefore load-bearing in exactly **two** places — the refusal in decision
4, and the schema flag in decision 6 — and nowhere in the read path. That is what keeps the static set
of decision 5 small enough to be safe.

### 4. A hierarchy-rooted team with no classes reads nothing, and is refused at save time

This is the epic's AC1 rule ("a team that has not written a query reads nothing rather than
everything", `ServiceNowWorkTrackingConnector.cs:111-125`) applied to the class dimension instead of
the query dimension, and it is the rule measurement 2 exists to justify.

It fires in **two** places, not one:

- `GetWorkItemsForTeam` returns `[]` with a warning naming the table — the same shape as the existing
  missing-query guard.
- `ValidateTeamSettings` returns a new pre-flight rung on `ServiceNowTeamQueryVerdict` —
  `missing_work_item_types`, pointing at the `WorkItemTypes` field. No IO.

The second is not belt-and-braces decoration. `isWorkItemTypesRequired` is a *hint to the web UI*;
`PUT /api/teams/{id}` accepts writes from clients that never read it (the CLI and the MCP server both
save team settings). A gate that lives only in the schema flag is a gate the API does not have.

### 5. "This table has descendants" is a static known-hierarchy set, in both stacks

`ServiceNowTableHierarchy.RootTables` on the backend, an exported constant beside the `Record` on the
frontend. Content today: **`task`**, and nothing else.

Not a `sys_db_object` lookup. ADR-116 measured that table **403** for every account below `itil`, so a
runtime answer would flip a settings field's visibility based on the customer's credentials — and it
cannot back a DTO that has to return the same schema to every caller. This restates ADR-116 decision 4
in the one place it would have been tempting to make an exception.

**The residual risk is stated rather than hidden.** A customer who roots at a hierarchy table Lighthouse
does not know about gets today's behaviour: the field stays hidden, no clause is emitted, and the read
covers the whole sub-hierarchy. That is not a regression — it is what every ServiceNow team does today
— but it *is* the D3 failure mode surviving in one corner. It is bounded by ServiceNow's own shape:
`task` is the work hierarchy, and everything the ITSM and ITSM-adjacent applications file lives under
it. Adding a root is a two-line code change in two files, which decision 7 makes loud.

### 6. `isWorkItemTypesRequired` becomes a function of the connection, on both stacks

Both schema factories take the **connection**, not the system type:

- `DataRetrievalSchemaDto.ForTeam(WorkTrackingSystems system, string workItemTable)` — **no default
  value on the new parameter**, so every call site is forced by the compiler to answer rather than
  inheriting `incident` semantics by omission.
- `getDefaultTeamSchema(connection: IWorkTrackingSystemConnection)` — the connection rather than two
  scalars, so the `Work Item Table` option-key string is looked up in exactly one place on the
  frontend, next to the hierarchy set, mirroring where the backend keeps it.

`getDefaultPortfolioSchema` takes the connection too, for symmetry; it ignores everything but the
system type, because ADR-116 decision 5 declines ServiceNow portfolios unconditionally.

The `useModifySettings` option and the `useCreateWizard` option widen in step. Both call sites already
hold the connection object — `useModifySettings.ts:332` inside `handleWorkTrackingSystemChange`, and
`useCreateWizard.ts:87` from `selectedConnection` — so nothing new has to be fetched, and no component
changes: `ModifyTeamSettings.tsx:76,190`, `CreateTeamWizard.tsx:74` and `useCreateWizard.ts:128` all
keep gating on `isWorkItemTypesRequired !== false`.

### 7. The twins are policed by a source-text enforcement test, not by a comment

A frontend enforcement test — the mechanism already used by
`Lighthouse.Frontend/src/utils/forecast/formatLikelihood.enforcement.test.ts` and
`deliveryJointLikelihoodDocs.enforcement.test.ts` — `readFileSync`s the two C# files and asserts:

1. the class-name set parsed out of `ServiceNowTableHierarchy.RootTables` equals the frontend constant,
   as a **set**, so drift in either direction fails;
2. the `Work Item Table` literal in `ServiceNowWorkTrackingOptionNames` equals the frontend option-key
   constant.

It runs under `pnpm test`, which is already a mandatory gate. The direction of the *read* is
one-way; the direction of *drift* caught is both, because the assertion is set equality.

The existing Bug #5613 guard (`SchemaFactories_EveryDeclaredWorkTrackingSystem_DoesNotUseTheQueryFallback`)
gains the new parameter and a second pass over the enum with a hierarchy-root table, so both branches
of the ServiceNow arm are covered by the exhaustiveness guard rather than only the leaf one.

### 8. A work item's `Type` is its own `sys_class_name`, with the configured table as fallback

`ServiceNowWorkItemMapper.MapRecord` reads `sys_class_name` from the record's universal form and uses
the configured table only when the field is absent or empty. For a leaf-rooted team the two are
identical by construction, so **no shipped team sees a changed `Type` and no data migration exists**
(AC-B2). For a `task`-rooted team the record's own class is the only answer that is not a lie.

The fallback is not defensive padding: `ReadForm` returns `string.Empty` for a missing field, and an
empty `Type` on every item would be a silent data change worse than the one being fixed.

Cost: **zero extra requests**. `sys_class_name` is already in every record of the connector's
`sysparm_display_value=all` read (measurement 3).

### 9. Metric definitions are scoped by class, not by table

`ServiceNowHistoryQuery.DefinitionQueryFor` takes the list of tables the definitions may sit on and
emits `table=<t>` for one and `tableIN<t1>,<t2>` for several — the same two-form rule as decision 2,
for the same reason, and `tableINincident,change_request` is directly measured (measurement 6).

For a leaf-rooted team with no classes the emitted query is byte-identical to today's. For a
`task`-rooted team it becomes the union over the named classes, which is what turns measurement 6's
**0 definitions** into 6.

This amends [ADR-118](./adr-118-servicenow-transition-history-from-metric-instance-spans.md) D2 in the
*scope* of the definition read only. Everything else in ADR-118 stands unchanged: spans still come from
`metric_instance`, still filtered by definition id, transitions still derived from each span's `start`,
the label still read from `value`.

### 10. At connection scope, a hierarchy root claims nothing about history

`ValidateConnection`'s capability probe (ADR-118 D5) asks `metric_definition` about the *connection's*
table. For a `task`-rooted connection that question has no meaningful answer, and the answer it does
return is actively wrong: `NoStateMetric`, whose message tells the administrator to "activate a Field
value duration metric definition on the state field of task" — advice that cannot be followed and that
contradicts what their teams will actually get.

So for a hierarchy-root table, `CapabilityOf` skips the definition read entirely and returns a success
carrying a new advisory, `history_determined_per_team`: this connection reads a table with several
record classes, and whether Lighthouse can see when work started is decided by the classes each team
names. One request saved, one false statement not made.

This is a new *message*, not a new `ServiceNowHistoryAvailability` member. The enum is what
`observedAvailability` and `SupportsTransitionHistory` branch on, and connection validation
deliberately does not write it.

## Alternatives Considered

**A. Per-team (table, query) pairs — N reads over N tables.**
Rejected at DISCUSS (D2) and confirmed by measurement: it multiplies the paging walk, the repeat
guard, the state choice list and the history resolution by N, and it needs a new persisted column plus
a migration across every provider because `Team` has no option bag. The class filter needs neither.

**B. Generate the `^OR` chain.**
Measured correct (measurement 1) and rejected anyway. `2n−1` conditions against an 8192-byte URL budget
already known to be the binding constraint, and a correctness that rests on a grouping rule observed on
one instance version. `IN` costs nothing to prefer.

**C. Emit a one-element `IN` for the single-class case, so there is only one code path.**
Genuinely tempting — one form, one test, no branch. Rejected because the one-element `IN` was not
measured and the `=` form was, and because emitting `IN` for a single class would change the wire
format of every currently-shipped leaf-rooted read for no behavioural gain. Revisit when the standing
integration guard measures it.

**D. Gate the class clause on `IsHierarchyRoot` rather than on "classes were named".**
Rejected. It would silently discard values a coach typed on a leaf-rooted team, which is the silent
no-op DoD 5 forbids, and it would put hierarchy-root knowledge into the read path where an unlisted
root becomes unrecoverable.

**E. Resolve "has descendants" at runtime from `sys_db_object`.**
Rejected on ADR-116's measurement (403 below `itil`), not on effort. The schema DTO must be the same
for every caller; a credential-dependent answer cannot back it.

**F. De-duplicate the schema twins — serve the schema per connection from a new endpoint, and let the
frontend `Record` become a pure offline fallback.**
This is the design that removes decision 7's guard by removing the duplication, and it is not obviously
wrong: the wizard already does async work on connection select, and a failed fetch could degrade to
today's `isWorkItemTypesRequired: false`, which is AC-B5-safe. Rejected for slice 01 on scope — it adds
a driving port, touches `useCreateWizard`, `useModifySettings` and every entity type, and Bug #5613
explicitly ruled that collapsing the tables is "a design change, not a fix". Recorded as an open
question rather than discarded; it is the right move if a second conditional flag ever appears.

**G. Split `isWorkItemTypesRequired` into separate "shown" and "required" flags.**
Would let the field be visible-but-optional for every ServiceNow team, which removes decision 5's
residual risk entirely: a customer on an unlisted hierarchy root could type classes and recover without
waiting for a release. Rejected for slice 01 — it is a shared-contract change across five systems and
two entity types, and it weakens AC-B5's "this story does not make the shipped configuration harder".
Recorded as an open question.

**H. Keep `MapRecord`'s `Type = table` and put the class somewhere else.**
Rejected: there is nowhere else. `WorkItemBase` has one type field, the UI groups by it, and for a
`task`-rooted team "the configured table" is the same string for an incident and a change request —
which is precisely the collapse this feature exists to undo.

## Consequences

**Positive.**
- One Lighthouse team per real team. The KPI the feature exists for becomes reachable.
- Every currently-shipped ServiceNow team is byte-identical on the wire: same URL, same query, same
  `Type`, same definition scope. AC-B5 and AC-B2 are structural, not asserted by inspection.
- Hierarchy-root knowledge touches two decision points and never the read path, so the static set is
  small, and being wrong about it is recoverable rather than corrupting.
- Slice 04's transition history survives the recipe this feature recommends.
- No migration, no new persisted column, no new HTTP route.

**Negative.**
- A third piece of ServiceNow knowledge is duplicated across the stacks (the hierarchy set and the
  option key, alongside the schema table itself). Mitigated by decision 7's guard; not eliminated.
- A hierarchy root Lighthouse does not know about still reads its whole sub-hierarchy. Bounded, stated,
  and addressable in a two-line change — but it is a real hole and it is the one an unusual customer
  will find first.
- The coach's real cost moves to **state mapping**, not the class list: four classes on the PDI carry
  14 distinct labels, and `Closed` is choice `3`, `7` and `107` depending on class. Because the
  connector maps by *label* (ADR-118), one "Closed" mapping covers all three — but a coach who maps one
  class's labels and stops loses the rest silently. On the PDI that is 61 change requests sitting in
  `Authorize`, 69 % of the class, reported only by `ReportStatesTheTeamNeverMapped` in a log. This is
  the feature's real usability risk and it belongs in the docs, loudly.
- `change_request` on a stock PDI carries **no state-tracking metric definition at all**, so "incidents
  and changes on one team" yields history for the incidents and none for the changes. An instance
  configuration fact, not a Lighthouse bug, and a documentation line.

## Related

- [ADR-114](./adr-114-servicenow-connection-validation-verdict-ladder.md) — the verdict ladder this
  slice adds two rungs to, and the functional-core/imperative-shell shape every new type here follows.
- [ADR-116](./adr-116-servicenow-table-at-connection-scope.md) — **amended**: decision 6's
  `isWorkItemTypesRequired: false` was recorded as the soft call C-3 "for deliberate revisit at slice
  02". This is that revisit. The flag becomes conditional; the table stays at connection scope.
- [ADR-118](./adr-118-servicenow-transition-history-from-metric-instance-spans.md) — **amended** in the
  scope of the definition read (decision 9) and in what connection validation claims for a hierarchy
  root (decision 10). D2's filter-by-definition rule itself is untouched.
- [ADR-124](./adr-124-servicenow-record-class-readability-ladder.md) — the per-class readability probe
  that makes a named-but-unreadable class visible instead of silently absent.
- SPIKE evidence: `docs/feature/servicenow-multi-table-work-item-types/spike/findings.md`.
- Locked handoff decisions S1–S4: `docs/feature/servicenow-multi-table-work-item-types/spike/wave-decisions.md`.
- Bug #5613: `docs/evolution/2026-07-30-bug-5613-schema-twin-drift.md`.
