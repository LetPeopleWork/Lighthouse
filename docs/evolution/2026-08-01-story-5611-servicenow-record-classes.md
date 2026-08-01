# Story 5611 — ServiceNow work item types as record classes

**ADO**: User Story [#5611](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5611),
parent Epic [#5513](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5513).
**Delivered**: 2026-07-31 (slice 01), finalized 2026-08-01.
**Workspace**: `docs/feature/servicenow-multi-table-work-item-types/` — retained; this document is the
summary, that directory is the history.

## What it was for

A ServiceNow team could read exactly one table. A flow coach whose team handles incidents *and*
changes — the way a Jira team has stories and bugs — had to split it into two Lighthouse teams, each
forecasting from half the throughput. The need came out of the maintainer's own dogfood of slice 02
on 2026-07-29, not from a backlog grooming: the mental model a ServiceNow user brings is that the
**table** is the kind of work and the **query** is the sub-filter within it, and Lighthouse models
"kind of work" as work item types on a team. The two models did not meet.

## The shape of the answer

`task` is ServiceNow's base table; `incident`, `change_request`, `problem` and `sc_request` all extend
it. So "several tables as work item types" became **one read rooted at `task`, filtered by
`sys_class_name`**, reusing the team's existing `Team.WorkItemTypes` — one query, one paging walk, one
repeat guard, one state choice list. The rejected alternative, per-team (table, query) pairs,
multiplies all four by N.

Three consequences the design did not start with:

- **Work Item Types became required for every ServiceNow team.** Rooting at `task` with no class
  filter reads the whole instance's work, so the field could not stay optional. The original promise
  that a leaf-rooted team would be unaffected was deliberately broken mid-delivery and the reasoning
  recorded rather than quietly dropped.
- **The `Work Item Table` connection option was deleted outright**, along with its default, its
  factory entry and the `ServiceNowTableHierarchy` set. Every read is now rooted at the constant
  `task`. Nothing ServiceNow has ever been released, so there is no migration and no cleanup code for
  a stored value nothing consults.
- **A work item's `Type` is its own `sys_class_name`**, not the configured table. Identical by
  construction for a leaf-rooted team, so no data migration.

Slice 02 (per-team table override) was **cancelled** — subsumed by the class filter.

## Decisions worth keeping

| | Decision | Why |
|---|---|---|
| D2 | Filter one `task`-rooted read by `sys_class_name`; do not read N tables | One query/paging walk/repeat guard/state list instead of N |
| D3 | Work Item Types is required once the root has descendants | Same rule as "a team with no query reads nothing rather than everything", applied to the class dimension |
| D7 | Class filter ships **before** the per-team override, reversing the ADO body | `Team` has no option bag — an override needs a new column and a migration on every provider, while `WorkItemTypes` is already persisted, in the DTO and rendered |
| — | Emit `sys_class_nameIN…`, never an `^OR` chain | Both were measured to give the identical answer; `IN` is one condition against the 8192-byte URL cliff and does not depend on `^OR` grouping precedence |
| — | The class ladder is **inverted**: probe `task?sys_class_name=X` first, the class's own table only when the first says zero | One round trip per correct class instead of two, and the first probe *is* the read rather than a proxy for it |

ADRs amended in place: **ADR-116** (decision 1 withdrawn), **ADR-123** (root collapses to one,
decision 5 withdrawn), **ADR-124** (decision 2 re-ordered with the measured per-rung table).

## What the SPIKE measured, and what it cost to learn

Everything below was measured against PDI `dev191338` and should not be re-derived:

- **`X-Total-Count` is ACL-blind.** A no-roles account gets header 103 / body 0 on `incident`. That
  gap *is* the AC-B6 detector — header > 0 with an empty body means ACL denial, and the class can be
  named. Header 0 (empty vs misspelt) is unresolvable, and `sys_db_object` does not rescue it.
- **A bogus `sys_class_name` narrows to zero rows, never widens** — the opposite of a bogus *field*
  name, which returns the whole table.
- **A `task`-rooted team loses all transition history unless the definition read is class-scoped** —
  `metric_definition` has zero rows for `table=task`. Stock `change_request` has no state-tracking
  definition at all, so changes never get spans regardless of Lighthouse code.
- **State mapping is the real usability risk, not the class list.** Four classes give 14 labels, and
  the same label carries different choice values per class (`Closed` = 3 / 7 / 107). Mapping by label
  is the only reason multi-class works — but a coach who maps one class's labels and stops loses the
  rest silently.

## Work completed

33 commits, `1c3cbf58c..fa3350e2d`, all on `main`. Full chain DISCUSS → SPIKE → DESIGN → DISTILL →
DELIVER in one day. Two defects found mid-delivery halted the `task`-rooting change and were fixed
rather than worked around, each with its own commit and live assertion: `resolved_at` was deleted (no
field the mapper reads is undeclared on `task`), and the paging sort was made total with a `sys_id`
tie-breaker after `number` was measured to be non-unique.

The delivery ran without `deliver/roadmap.json` or `deliver/execution-log.json` — the slices were
driven directly rather than through the roadmap machinery — so git history and
`slices/slice-01-work-item-types-as-record-classes.md` are the completion evidence for this
finalization, not an execution log.

## Quality gates

| Gate | Result |
|---|---|
| Backend mutation | **88.44 %** (454 killed / 45 survived / 5 timeout, 504 tested) |
| Frontend mutation | **85.71 %**, scoped to the changed regions of `DataRetrievalSchemaDefaults.ts` |
| `dotnet build` | clean, zero warnings |
| `dotnet test` | 4239 / 4239 |
| SonarCloud | green at `fa3350e2d` |

Mutation record, with the configs that produced it:
`docs/feature/servicenow-multi-table-work-item-types/mutation/`.

Two long-standing Stryker beliefs were disproved during this run and are corrected there: `13547
mutants created` is a **pre-filter** count, not evidence of a broken `mutate` glob, and the eleven
minutes before mutation began were a **missing `test-case-filter`**, not a glob failure.

## Definition of Done — item by item

| # | Item | Status |
|---|---|---|
| 1 | Both slices' ACs green | **Met for slice 01.** Slice 02 cancelled (D2 subsumes it) |
| 2 | `isWorkItemTypesRequired` asserted on both stacks; #5613 guard passes | Met |
| 3 | Mutation ≥ 80 % both stacks | Met — 88.44 % / 85.71 % |
| 4 | No new Sonar issues; both builds warning-free | Met |
| 5 | Verified against a real instance, not only fixtures | Met — PDI `dev191338`, live assertions per ladder rung |
| 6 | Docs updated (`docs/` ServiceNow page: Work Item Types content + `task`-root recipe) | **NOT met — carried to #5578.** There is no public ServiceNow page to update: the connector is unreleased and its user documentation is Epic 5513 slice 05's deliverable. The content this story owes it is recorded above and in the workspace |
| 7 | A dogfood moment the same day the slice lands | Met — found the two defects that halted `task`-rooting |
| 8 | Epic 5513 delta gets a back-reference once 5577 finalized | Met in this change — 5577 finalized at `0e2e78340` |
| 9 | ADO #5611 transitioned; Release Notes tag decided | **Open** — maintainer's call |

## Finalization checklist

- **Docs prose** — not written; see DoD 6. Deferred to #5578 by the epic's own plan, not skipped.
- **Screenshots** — the settings screen *does* visibly change (Work Item Types is now always shown and
  required for ServiceNow). No shot taken, because there is no page to host it. Travels with the docs
  to #5578.
- **Demo data** — done during delivery: `1a6cf13f1` seeds change requests and problems alongside
  incidents, so a demo instance exercises the multi-class path.
- **Website marketing surface** — untouched. No asset under `docs/assets/` was added, renamed or
  deleted, so nothing letpeople.work hot-links via jsDelivr is affected.
- **Lighthouse-Clients (CLI / MCP)** — no client-facing contract changed. `workItemTypes` was already
  on the team payload; `DataRetrievalSchemaDto` drives the web settings UI only. No version bump
  prepared — worth a maintainer confirmation before the release, since it is an assertion about
  another repo.
- **RBAC** — unchanged. No endpoint, permission or gating surface was added; every change sits behind
  the existing team-settings and connection routes.

## Known follow-ups

- **Bug #5621** — three defects from the second review, filed rather than fixed. F1 (blocker):
  `WorkStartedFor` / `WorkFinishedFor` test for *any* span while the mappers filter to *state* spans,
  so a record with only non-state spans gets `null` dates, silently. F2: `FindLast(Done)` mis-dates
  contiguous Done spans. F3: `InAStableOrder` early-returns when the team's query already has
  `ORDERBY`, so the `sys_id` tie-breaker never reaches those teams. F4 and F5 are parked on the item.
- **#5612** — the work item `Type` should carry the class *label*, not only its name.
- **#5610** — query-authoring guidance and a Visual Task Board picker; DISCUSS done, DESIGN was gated
  on this story landing.
- **Reading tables that are not one hierarchy** (e.g. `incident` with `rm_story`) is out of scope and
  unexpressible under D2. Named as the successor if a shop reports the need.

## Related

- `docs/evolution/2026-07-31-epic-5513-servicenow-slice-04-time-in-state.md`
- `docs/evolution/2026-07-30-bug-5613-schema-twin-drift.md` — why D6 touches both schema twins
- ADR-116, ADR-123, ADR-124 under `docs/product/architecture/`
