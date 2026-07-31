# ServiceNow: several kinds of work on one team — feature delta

**ADO**: User Story [#5611](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5611), parent Epic
[#5513](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5513) (ServiceNow Integration).
**Waves recorded here**: DISCUSS (2026-07-31).

**Why this file and not the epic's.** Epic 5513's `feature-delta.md` is being appended to by the
parallel finalization of Story 5577 (slice 04, transition history). 5611 is a post-DESIGN story filed
out of the slice-02 dogfood, not one of the epic's slices 01–05, so it gets its own workspace and its
own delta. The epic delta stays the record for slices 01–05; this file is the record for 5611.

---

## Wave: DISCUSS / [REF] Persona and job

**Persona**: `flow-coach` (`docs/product/personas/flow-coach.yaml`) — the person who owns a Lighthouse
team and points it at the work. Secondary: `config-admin`, who owns the connection and therefore the
table it is rooted at.

**JTBD one-liner** — new SSOT job `job-snow-flow-coach-one-team-several-kinds-of-work`:

> When my team handles more than one kind of ServiceNow record — incidents *and* changes, the way a
> Jira team has stories and bugs — I want one Lighthouse team to cover all of them, so my flow
> metrics describe my team's actual workload instead of the one record type Lighthouse let me pick.

Extends `job-snow-flow-coach-see-flow-metrics` (epic 5513). That job is served: a ServiceNow team gets
throughput, cycle time and forecasts. What it does not serve is a team whose workload is not one
table. Today that coach either creates a second Lighthouse team per record type — which splits the
throughput of one team into two half-teams and makes the forecast wrong — or measures one kind of work
and ignores the rest.

**Where the need came from**: maintainer dogfood of slice 02, 2026-07-29, recorded in
`docs/feature/epic-5513-servicenow-integration/feature-delta.md` (Dogfood findings, "#5611" row). The
mental model a ServiceNow user brings: the **table** is the kind of work, the **query** is the
sub-filter within it. Lighthouse models "kind of work" as work item types on a team. The two models
did not meet, because a team could read exactly one table.

---

## Wave: DISCUSS / [REF] Locked decisions

D-numbers are local to this feature. Epic 5513's own D1–D11 are referenced as "epic D*n*".

| ID | Decision | Rationale |
|---|---|---|
| **D1** | Split 5611 into two slices: **Work Item Types as record classes** and **per-team table override**. | The ADO body proposes the split and the two are genuinely separable — one answers the dogfood, the other closes a delivered-vs-designed gap. Maintainer, 2026-07-31. |
| **D2** | "Several tables as work item types" means **filtering one read by `sys_class_name`**, not N reads over N tables. | `task` is ServiceNow's base table; `incident`, `change_request`, `problem`, `sc_request`, `sc_req_item` all extend it. A team rooted at `task` and filtered to `sys_class_name=incident^ORsys_class_name=change_request` gets exactly "incidents and changes" from one read. One query, one paging walk, one repeat guard, one state choice list. The (table, query)-pairs alternative multiplies all four by N and was rejected for it. Maintainer, 2026-07-31. |
| **D3** | When the configured table has descendants, **Work Item Types becomes required**, not optional. | Rooting at `task` with no class filter reads the entire task hierarchy — the whole instance's work. This is AC1's rule ("a team that has not written a query reads nothing rather than everything", `ServiceNowWorkTrackingConnector.cs:111-125`) applied to the class dimension instead of the query dimension. Raised by the maintainer as "if we use task, we may get everything?" — correct, and it is the reason the field cannot stay optional. |
| **D4** | A work item's `Type` becomes its **`sys_class_name`**, replacing "the configured table". | `ServiceNowWorkItemMapper.MapRecord(record, owner, table)` sets `Type = table` today. For a team rooted at a leaf table the two are identical by construction (`incident` records carry `sys_class_name = incident`), so single-table teams see no change and no data migration is needed. For a `task`-rooted team the record's own class is the only answer that is not a lie. |
| **D5** | `ValidateConnection` keeps probing the **connection**-scope table. A per-team override cannot replace it. | Validation runs before any team exists, so a team-scope-only table leaves the probe with nothing to read and makes the epic's AC4 third failure ("reachable instance, no read access to the table") structurally unreachable. This is exactly why ADR-116 rejected Option B. Restates epic DESIGN Open Call 3 option C. |
| **D6** | `isWorkItemTypesRequired` goes **conditional on the configured table**, and changes in **both** schema twins. | C-3 in the epic delta predicted this and Bug #5613 proved the cost estimate wrong: the flag both skips validation *and* hides the field, and the backend `switch` (`DataRetrievalSchemaDto.cs`) and the frontend `Record` (`DataRetrievalSchemaDefaults.ts`) are duplicated knowledge that already drifted once and shipped unsaveable ServiceNow teams. The enum-exhaustiveness guard added in `cb5f0efb0` fails if only one side is touched — this feature must not try to route around it. See `docs/evolution/2026-07-30-bug-5613-schema-twin-drift.md`. |
| **D7** | **Work Item Types as record classes ships first**, the per-team table override second. | Reverses the order the ADO body implies. Three reasons, all found while writing this delta rather than before it: (1) `Team` has no option bag — it implements `IWorkTrackingSystemOptionsOwner`, which is only `WorkTrackingSystemConnectionId` plus the navigation property — so the override needs a new persisted column and a migration across every supported provider, while the class filter reuses `Team.WorkItemTypes`, already persisted, already in the DTO, already rendered and merely hidden. DESIGN's "one option key" estimate was made at *connection* scope, where an option bag exists. (2) The class filter is what the dogfood actually asked for — "incidents and changes, like stories and bugs in Jira" is one team, several kinds; the override serves a shop that wants them on different teams. (3) Every open call (OC-1, OC-2, OC-3) rides on the class filter, so failing first costs one slice instead of two. Maintainer, 2026-07-31. The case for the other order, stated fairly: the override is the older debt, it is independent of every open call, and it would prove the team-scope concept before the harder model rides on it. |

---

## Wave: DISCUSS / [REF] Out of scope

- **Reading tables that are not one hierarchy** — e.g. `incident` (ITSM) together with `rm_story`
  (Agile Development 2.0). D2's model cannot express it: there is no common ancestor to root at.
  Named as the successor if a shop reports the need; nothing is known today that says one will.
- **Portfolios over ServiceNow.** Slice 03 was cancelled, `GetParentFeaturesDetails` throws
  permanently (`ServiceNowWorkTrackingConnector.cs:322-325`), and the schema declares portfolios
  unsupported. Unchanged by this feature.
- **A picker for the class list.** Work Item Types is a hand-typed list for every connector Lighthouse
  ships. Making it a dropdown for ServiceNow alone is a different story (and overlaps #5610's
  "pre-fill from a Visual Task Board" idea).
- **Write-back of any kind** — epic D8, still read-only.

---

## Wave: DISCUSS / [REF] Pre-requisites

1. **Story 5577 (slice 04, transition history) must land and push first.** Its refactors are in flight
   against `ServiceNowWorkTrackingConnector` in the main checkout (`7dbdf4a49` and predecessors,
   unpushed at the time of writing) — the same file all three `ResolveWorkItemTable` call sites live
   in. Doc waves are safe in parallel; code is not.
2. **The Bug #5613 schema-twin guard is in place** — shipped `cb5f0efb0`. D6 depends on it.
3. **A ServiceNow instance with more than one populated task-descendant class** to verify OC-1 and
   OC-2 against. The PDI seeded by Story 5572 is the candidate; whether its `change_request` table has
   records is unverified.

---

## Wave: DISCUSS / [REF] Driving ports

| Surface | Change |
|---|---|
| `PUT /api/teams/{id}` (team settings) | Carries `workItemTypes` already. Slice B makes the field *visible and required* for a hierarchy-rooted ServiceNow team; slice A would add a new team-scope table field. |
| Team settings screen + Create Team wizard | `ModifyTeamSettings.tsx:76,190` and `CreateTeamWizard.tsx:74` already gate the Work Item Types block on `isWorkItemTypesRequired !== false`. Slice B flips the input, not the components. |
| `GET /api/.../dataretrievalschema` (`DataRetrievalSchemaDto`) | The conditional from D6 — both twins. |
| ServiceNow Table API (outbound) | `sysparm_query` gains a leading `sys_class_name=…^OR…^` clause ahead of the team's own query. |

---

## Wave: DISCUSS / [REF] User stories

### Story A — one connection, teams on different tables

**As** a configuration administrator whose ServiceNow instance serves several Lighthouse teams,
**I want** each team to be able to read a different table from the one the connection defaults to,
**so that** an incident team and a change team can share one connection and one credential.

`job_id: job-snow-admin-connect-servicenow` (secondary: `job-snow-flow-coach-one-team-several-kinds-of-work`)

#### Elevator Pitch
Before: every team on a ServiceNow connection is pinned to the connection's table, so a second kind of work means a second connection and a second credential to get approved.
After: open Team Settings → set **Work Item Table** to `change_request` → Save → the team's Work Items tab fills with change requests while the incident team on the same connection is untouched.
Decision enabled: whether one approved ServiceNow credential can cover the whole department, or the admin has to go back to the platform team for another.

**AC-A1** — A team with no table of its own reads the connection's table. Existing teams are
unaffected; no migration of data, only of schema.
**AC-A2** — A team with a table of its own reads *that* table in `GetWorkItemsForTeam` and in
`ValidateTeamSettings`, and the connection's table in neither.
**AC-A3** — `ValidateConnection` still probes the **connection** table (D5). Setting a team override
does not change what the connection validates against, and a connection with no team still validates.
**AC-A4** — A team override naming a table the account cannot read fails `ValidateTeamSettings` with
the table named in the message — not as an empty team.

*Note the honest limit*: under D2 this story does **not** answer the dogfood finding. A shop that
wants incidents *and* changes on **one** team is served by Story B, not by this. Story A serves a shop
that wants them on *different* teams, and closes the gap between DESIGN Open Call 3 option C (which
accepted a per-team override "in slice 02") and what slice 02 shipped.

---

### Story B — one team, several kinds of work

**As** a flow coach whose team handles incidents and changes together,
**I want** to name the record classes my team works on,
**so that** my throughput is my team's throughput and not one record type's share of it.

`job_id: job-snow-flow-coach-one-team-several-kinds-of-work`

#### Elevator Pitch
Before: a team can read exactly one ServiceNow table, so a team that handles incidents and changes must be split into two Lighthouse teams whose forecasts are each computed from half the work.
After: set the connection's table to `task`, open Team Settings → **Work Item Types** → type `incident` and `change_request` → Save → the Work Items tab shows both, each row labelled with its own kind.
Decision enabled: whether the team's throughput and forecast can be trusted as the team's, rather than as one queue's slice of it.

**AC-B1** — A team rooted at a hierarchy table with `["incident", "change_request"]` syncs records of
both classes, and no records of any other class in that hierarchy.
**AC-B2** — Each synced item's `Type` is its own `sys_class_name`, not the configured table (D4). A
team rooted at a leaf table sees exactly the `Type` it saw before this story.
**AC-B3** — A team rooted at a hierarchy table with an **empty** Work Item Types list reads **nothing**
and says why, in the shape `GetWorkItemsForTeam` already uses for a missing query
(`ServiceNowWorkTrackingConnector.cs:118-125`). It never reads the whole hierarchy (D3).
**AC-B4** — The team settings screen and the create-team wizard **show** the Work Item Types field for
a hierarchy-rooted ServiceNow team and **reject the save** when it is empty — and the backend and the
frontend agree about that, asserted in both schema twins (D6, Bug #5613).
**AC-B5** — A leaf-rooted ServiceNow team (`incident` alone, the shipped default) keeps hiding the
field and keeps saving without it. This story does not make the shipped configuration harder.
**AC-B6** — A class named in Work Item Types that does not exist, or that the account cannot read,
produces a specific message naming the class — not an empty team and not a silent subset.

---

## Wave: DISCUSS / [REF] Definition of Done

1. Both slices' ACs green, backend and frontend.
2. `isWorkItemTypesRequired` conditional asserted on **both** stacks; the #5613 exhaustiveness guard
   still passes.
3. Mutation testing ≥ 80% on the changed backend and frontend surface (project standing gate).
4. No new SonarCloud issues; `dotnet build` and `pnpm build` warning-free.
5. Verified against a real instance — a `task`-rooted team returning at least two classes — not only
   against fixtures. The epic's dogfood discipline: 164 tests did not find what one manual run did.
6. Docs updated per-feature (`docs/` ServiceNow page): what to put in Work Item Types, and the
   `task`-root recipe. Screenshot only if the settings screen visibly changes.
7. A dogfood moment on the same day the slice lands.
8. Epic 5513's `feature-delta.md` gets a back-reference to this file once 5577 has finalized — not
   before, to avoid an append conflict.
9. ADO #5611 transitioned; Release Notes tag decided with the maintainer.

---

## Wave: DISCUSS / [REF] Open calls

| ID | Question | Why it is open | Settle by |
|---|---|---|---|
| **OC-1** | Does `sys_class_name=incident^ORsys_class_name=change_request` behave as expected against `/api/now/table/task` on a real instance, combined with the team's own encoded query? | The whole model rests on it. Encoded-query semantics for `^OR` grouping are order-sensitive in ServiceNow — an `^OR` chain can bind wider than intended when followed by further `^` terms, which would silently widen the read. **Probe `sys_class_nameINincident,change_request` alongside it**: `IN` is a single condition, so it sidesteps `^OR` precedence entirely rather than answering the question about it. If the `IN` form agrees with reading each class from its own table and the `^OR` form does not, `^OR` must never be generated and OC-1 dissolves instead of being settled. | SPIKE, against the PDI, before DESIGN. |
| **OC-2** | ServiceNow ACLs are evaluated per record class. A restricted account reading through `task` gets the classes it may read and **silently omits** the rest. Does that show up anywhere the coach can see it? | Epic D11 makes least privilege a design requirement, and the epic's whole anxiety is "quietly wrong beats visibly missing". Also interacts with `ValidateTeamSettings`'s two-probe widening detector, whose "everything" probe would now count the entire hierarchy. Probe it by reading the same query twice — once with the least-privileged account a platform team would grant, once with a fuller one — and comparing row counts **per `sys_class_name`**. | SPIKE, against the PDI, before DESIGN. |
| **OC-3** | Does Work Item Types take class **names** (`incident`) or **labels** (`Incident`)? | State mapping already learned this lesson the hard way — ServiceNow answers with numeric choice values and separate display labels, which is why `sysparm_display_value=all` earned its place. `sys_class_name` is a name, but the field the coach reads in the UI is a label. | DESIGN. |

---

## Wave: DISCUSS / [REF] Slices and prioritization

Two slices, briefs in `slices/`, ordered per D7.

| # | Slice | Ships | Learning hypothesis |
|---|---|---|---|
| 01 | Work Item Types as record classes | A coach's one team covers incidents and changes | Disproves D2 if `^OR` class filtering on `task` does not hold against a real instance (OC-1), or if ACL-filtered reads are indistinguishable from correct ones (OC-2) |
| 02 | Per-team table override | An admin points two teams on one connection at two tables | Disproves "the per-team override is the cheap half" — the phrase the ADO body uses — since it needs a persisted column and a migration where slice 01 needed neither |

Slice 01 first because it carries every open call, needs no migration, and is the slice the dogfood
asked for. Full rationale and the counter-argument: D7 above.

If OC-1 fails, D2 collapses and slice 01 is re-scoped to per-team (table, query) pairs — at which
point slice 02 stops being a separate slice, because a team that names its own table is a degenerate
case of a team that names several. That coupling is why the order matters at all.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measured by |
|---|---|---|
| A ServiceNow team can cover every record class its shop handles | 1 Lighthouse team per real team, not per record class — verified on the dogfood instance | Manual dogfood, recorded in the slice's DELIVER notes |
| No team is ever created unable to save | 0 occurrences | The #5613 guard plus AC-B4 on both stacks |
| No read silently widens to the whole hierarchy | 0 occurrences | AC-B3 + the `ValidateTeamSettings` widening probe |
| Type fidelity for existing teams | 0 changed `Type` values for leaf-rooted teams | AC-B2 |

Self-hosted instances phone nothing home (`project_self_hosted_telemetry_gap`, ADO #5015), so every
KPI here is verified by test and by dogfood, not by field telemetry. Stated rather than skipped.

---

## Wave: DISCUSS / [REF] Scope Assessment: PASS

Two stories, two slices, one bounded context (work-tracking connectors), two modules (backend
connector + frontend settings schema), no new integration points — the ServiceNow Table API is
already spoken. Under every oversized heuristic.

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Verdict |
|---|---|---|
| 1 | Business value articulated | ✓ Both stories carry an elevator pitch with a decision enabled |
| 2 | Job traceability | ✓ Story B → new SSOT job; Story A → `job-snow-admin-connect-servicenow` |
| 3 | Acceptance criteria testable | ✓ 4 + 6 ACs, each with an observable outcome |
| 4 | Dependencies identified | ✓ Pre-requisites section — 5577 lands first, #5613 guard in place |
| 5 | Sized | ✓ Slice 01 ≈ 1 day, no migration; slice 02 ≈ 1 day + an expand-only migration |
| 6 | No open blocking questions | ⚠ OC-1 and OC-2 must be closed in DESIGN before slice 01 is buildable. Not blocking DISCUSS handoff; blocking slice-01 DELIVER. Slice 02 is blocked by neither |
| 7 | Out-of-scope explicit | ✓ |
| 8 | Persona identified | ✓ flow-coach primary, config-admin secondary |
| 9 | Definition of Done agreed | ✓ 9 items above |

**Handoff**: DESIGN (`nw-solution-architect`), whose first job is OC-1 and OC-2 against a live
instance. DEVOPS takes the KPI section only; no infrastructure change is implied by either slice.

---

## Wave: SPIKE / [REF] Open calls settled — 2026-07-31

Run against the epic's PDI. Evidence and measurements: `spike/findings.md`; promotion decision
(DISCARD — the findings are the deliverable, slice 01 goes through DISTILL as planned):
`spike/wave-decisions.md`.

| ID | Verdict |
|---|---|
| **OC-1** | **Settled, both ways.** `^OR` chain and `IN` both return the reference answer exactly — identical `sys_id` sets across four team queries, including one carrying its own `^OR` and one carrying the connector's `ORDERBY`. The tie breaks on URL budget and on not depending on a grouping rule seen on one instance: **generate `sys_class_nameIN…`**. D3 confirmed — unfiltered, the same team reads 579 records of 13 classes instead of 159 of 2. |
| **OC-2** | **Settled, and the answer is "no, not without help".** An account that may read `incident` but not `problem` gets 200 with the `problem` rows simply absent. But `X-Total-Count` is **ACL-blind**, so header > 0 with an empty body is a denial Lighthouse *can* name: AC-B6 becomes one `sysparm_limit=1` probe per named class at validation time. Header = 0 stays ambiguous (empty class vs misspelt name) and cannot be resolved for the accounts that matter. |
| **OC-3** | **Settled early, at zero cost.** Class **names** (`change_request`), never labels — `sysparm_query` matches the stored value, and `sys_class_name` already rides in the connector's `display_value=all` read, so D4 costs no extra request. |

Two things the open calls did not ask, found anyway, both binding on DESIGN:

- **A `task`-rooted team loses transition history entirely.** `metric_definition` has 0 rows for
  `table=task` — definitions attach to concrete classes only. Slice 01 must scope
  `ServiceNowHistoryQuery.DefinitionQueryFor` to the classes (`tableIN…`); added to slice 01's IN
  scope. Separately, `change_request` on a stock PDI has no state-tracking definition at all, which
  is an instance fact for the docs.
- **State mapping, not the class list, is this feature's usability risk.** Four classes carry 14
  distinct labels, and the same label has different choice values per class (`Closed` = 3 / 7 / 107).
  Lighthouse maps by label, which is what makes that survivable — but a coach who maps one class's
  labels and stops loses the rest silently (61 change requests in `Authorize`, on the PDI).

One pre-existing defect recorded so it is not re-derived: `ValidateTeamSettings` compares two
ACL-blind `X-Total-Count` values, so a no-roles account passes the widening comparison while reading
nothing. Connection validation catches that account a rung earlier; this feature changes only the
denominator (`everything` becomes the whole hierarchy), which DESIGN must rule on.
