# Feature Delta — epic-5687-faster-updates

**ADO**: Epic #5687 "Faster Updates" (Planned, tags `Community; Documentation; Release Notes`, created
2026-08-06, no children at DISCUSS start; eight child Stories #5724-#5731 created at wave close) · **Feature type**: backend (work-tracking fetch) with a
cross-cutting observability edge · **Density**: lean, plus two accepted expansions —
`alternatives-considered` and `persona-narrative` (both [WHY], rendered at wave end) ·
**DISCUSS run**: 2026-08-08

The epic is a five-line sketch with one concrete complaint in it — *"looking at you, on prem jira
instance with 25 years of history"* — and two open questions the author already saw coming: what happens
when the config changes, and which settings do not warrant a remote fetch at all. This wave turns that
into locked decisions. Three things the sketch did not say are what the wave is worth:

1. **The deletion rule is what makes delta hard.** `WorkItemService` removes any stored item the fetch
   did not return. A naive `updated >= lastSync` query would silently make every item immortal.
2. **Staleness is time-driven, not change-driven.** `WorkItemBecameStale` fires from a comparison
   against `CurrentStateEnteredAt` — evaluated today only for *fetched* items. Delta fetching would
   stop stale items from ever being recognised as stale. This is the sharpest trap in the epic.
3. **Only the remote fetch is deltaed.** Remaining-work rollup, extrapolation and forecasts must keep
   recomputing every cycle, because they depend on wall-clock and on other teams, not on this team's
   fetch.

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ⊘ `docs/feature/epic-5687-faster-updates/discover/` (not found — no DISCOVER wave ran)
- ⊘ `docs/feature/epic-5687-faster-updates/diverge/` (not found — no DIVERGE wave ran)
- ✓ `docs/product/jobs.yaml` (schema_version 1, 89 jobs) — none covers sync cost, sync duration, or
  refetch scope. Nearest neighbours are `job-react-proactively-to-workitem-change` (what the sync
  *emits*) and `job-operator-observe-in-cluster` (how an operator *watches* the instance); neither says
  anything about what the sync *costs*.
- ✓ `docs/product/journeys/` (37 journeys) — none touches update performance.
- ✓ `docs/product/personas/` (9 personas) — `platform-operator` and `config-admin` are reused verbatim;
  no new persona needed. `flow-coach` appears only as the party whose data must stay correct.
- ✓ `docs/product/architecture/` (131 ADRs, read by index) — none constrains fetch strategy. ADR-027
  (domain-event dispatcher is transport-only, recovered on the next re-sync) is the one that *touches*
  this: it assumes a re-sync re-derives everything, which delta changes, so D10 below is written to keep
  that assumption true.
- ⊘ `docs/product/vision.md`, `docs/project-brief.md`, `docs/stakeholders.yaml` (not found — this repo
  carries product SSOT under `docs/product/` instead)
- ✓ `CLAUDE.md`, `docs/ci-learnings.md` — standing rules applied (expand-only migrations, configurable
  terminology, per-feature docs, quality gates, Jira DC duplicate-ReferenceId hazard 2026-05-25).
- ✓ **ADO Epic #5511 "Task Manager"** (New) — read because D4 defers this epic's UI to it. #5511 wants a
  task-manager view of running/queued updates with cancel, plus a general admin health dashboard.

No DISCOVER evidence exists to contradict, so no contradiction check was possible and none is claimed.
Everything in the Current-State Surface Inventory below was read from code during this wave.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this feature |
|---|---|
| `platform-operator` | Primary. Owns the instance and the relationship with the work-tracking system it hammers. Reads the logs. |
| `config-admin` | Edits Team/Portfolio settings; carries the "did my edit just cost a 25-year refetch?" anxiety. |
| `flow-coach` | Affected, never acting. Their metrics must stay correct — they are the reason delta cannot cut corners. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-operator-sync-without-hammering-the-tracker` | When Lighthouse refreshes against a tracker holding decades of history, fetch only what actually moved, so a short refresh interval stops being a cost decision. |
| `job-config-admin-know-which-settings-cost-a-refetch` | When I change a setting, let Lighthouse work out by itself whether the data has to be pulled again — and pull nothing when the change is purely local. |
| `job-operator-read-an-update-log-that-says-something` | When I read the log to understand an update, give me one line per update saying what it did and what it cost, not a per-entity stream I have to filter. |

Full job stories, dimensions, four forces and opportunity scores are written to
`docs/product/jobs.yaml` (SSOT). Opportunity ranking:

| Job | Importance | Satisfaction | Gap | Order |
|---|---|---|---|---|
| `job-operator-sync-without-hammering-the-tracker` | 4 | 1 | **3** | 1st |
| `job-config-admin-know-which-settings-cost-a-refetch` | 3 | 1 | **2** | 2nd |
| `job-operator-read-an-update-log-that-says-something` | 3 | 1 | **2** | 2nd (ships first — see D5) |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from code during this wave. Every claim below has a file:line.

| Fact | Evidence |
|---|---|
| A team update refetches the **entire** query result every cycle | `WorkItemService.cs` `RefreshWorkItems` |
| A portfolio update does the same for Features, then again for parent Features | `WorkItemService.cs` `RefreshFeatures` / `RefreshParentFeatures` |
| Removal rule: stored item absent from the fetch → deleted | `WorkItemService.cs` `storedWorkItems.RemoveAll` + `workItemRepository.Remove` |
| The remote query is `(user query) AND types AND states AND resolved-cutoff` — identical shape on Jira and ADO | `JiraWorkTrackingConnector.cs:1235`, `AzureDevOpsWorkTrackingConnector.cs:962` |
| A `DoneItemsCutoffDays` window already bounds *closed* items — it does nothing for open ones | `JiraWorkTrackingConnector.cs:1244` |
| Jira's cost driver is the changelog: any issue with >30 entries triggers extra paged requests at 100/page | `JiraWorkTrackingConnector.cs:1120`, `:776-838` |
| ADO already runs two-phase (WIQL → id refs, then chunked `GetWorkItemsAsync`) | `AzureDevOpsWorkTrackingConnector.cs:641`, `:684-696` |
| ADO's cost driver is `GetRevisionsAsync` **per work item** to rebuild transitions | `AzureDevOpsWorkTrackingConnector.cs:811-839` |
| A work item carries **no** remote-changed timestamp today | `Models/WorkItemBase.cs:18-49` |
| Updates are periodic (`minutesSinceLastUpdate >= RefreshAfter`) or manually triggered from a controller — **saving a setting does not itself trigger a fetch** | `TeamUpdater.ShouldUpdateEntity`, `UpdateServiceBase.UpdateAll`, `TeamController.cs:86` |
| Duration + item count are already recorded per update | `TeamUpdater.cs` → `RefreshLog` |
| `UpdateAll` logs one Information line **per entity per cycle** before deciding not to update it | `UpdateServiceBase.cs:88`, `TeamUpdater.ShouldUpdateEntity` |
| "Updating Work Items for Team {X}" is emitted **three times** per team per update | `WorkItemService.UpdateWorkItemsForTeam`, `.RefreshWorkItems`, `JiraWorkTrackingConnector.cs:114` |
| 38 Information/Debug statements sit on the update path; 17 in `WorkItemService` alone | `grep -c LogInformation` across the update path |
| Staleness is derived from wall-clock vs `CurrentStateEnteredAt`, evaluated only for **fetched** items | `WorkItemService.IsStale` / `AddStalenessEventIfThresholdCrossed` |
| `WorkItem.Update(item)` is a field-copy — `init`-only / `[NotMapped]` members do not survive it | `Models/WorkItem.cs`, and `project_workitem_sync_transient_and_state_mapping_gotchas` |

---

## Wave: DISCUSS / [REF] Connector Capability Matrix

The user asked for **all** connectors to be examined in this epic before deciding how the non-Jira ones
are scheduled. This is that examination. "Sweep" = a cheap query returning identity + remote-changed
timestamp for the full result set.

| Connector | Remote-changed field | Sweep is possible? | Cost driver removed by delta | Verdict |
|---|---|---|---|---|
| **Jira Cloud** | `updated` (JQL + `fields=updated`) | Yes — same JQL, one field | Full field set + `expand=changelog` + per-issue changelog paging (`:776`) | **In scope.** Biggest single win. |
| **Jira Data Center** | `updated` | Yes, **but** offset pagination over an unordered JQL is the known duplicate-`ReferenceId` hazard (ci-learnings 2026-05-25) | Same as Cloud, and this is the epic's named instance | **In scope, after a stability probe** (OQ-1). |
| **Azure DevOps** | `System.ChangedDate` | Yes — a second WIQL `AND [System.ChangedDate] >= @x`; the plain WIQL already gives the full id set for free | `GetRevisionsAsync` per item (`:811`) — the dominant cost | **In scope.** Structurally the smallest change. |
| **ServiceNow** | `sys_updated_on` | Yes — `sysparm_query` already carries an ordering clause (`InAStableOrder`), and `sysparm_fields` can narrow to id+timestamp | Per-record state-span reads (`ReadSpans`, `ReadHistory`) | **In scope**, lower value — PDI-scale data, and paging already guarded. |
| **Linear** | `updatedAt` | Yes — GraphQL `filter: { updatedAt: { gt: … } }`, and the history fragment is what costs (`HistoryConnectionFragment`) | History connection per issue/project | **In scope**, lowest value — Linear's API is already the fastest of the four. |
| **CSV** | — | **No** | Nothing. The "fetch" is a user-uploaded file. | **Out of scope, permanently** (D11). |

Consequence: the delta contract is expressible on every remote connector. Nothing in the matrix forces a
different design per connector, which is why D1 can be a single contract rather than four.

---

## Wave: DISCUSS / [REF] Locked Decisions

- **[D1] Two-phase fetch: cheap identity sweep, then payload for the changed only.** Every cycle still
  issues the *same* full query, but phase 1 asks only for `(id, remote-changed-timestamp)`. Phase 2
  fetches the full payload — fields, changelog, revisions, spans — only for ids whose timestamp differs
  from what is stored. *Rejected*: `updated >= lastSync` plus a periodic reconcile (leaves ghost items
  for up to N cycles and makes correctness a scheduling parameter); delta plus a manual "Full refresh"
  button (makes correctness a user chore). User decision, 2026-08-08.
- **[D2] Removal semantics do not change.** Because phase 1 enumerates the full result set every cycle,
  `removed = stored − sweepIds` keeps exactly today's meaning. No item can outlive its query. This is
  the whole reason D1 was chosen over the cheaper alternatives, and it is a hard acceptance rule for
  every connector slice.
- **[D3] A fetch fingerprint decides when a full fetch is required.** Hash the properties that shape the
  remote query — `DataRetrievalValue`, `WorkItemTypes`, `AllStates`, `DoneItemsCutoffDays`, additional
  field definitions, parent-override field, `WorkTrackingSystemConnectionId`. Stored per Team/Portfolio.
  Mismatch → the next cycle is a full fetch. Everything else (wait states, blocked rules, staleness
  threshold, named cycle times, ordering policy, terminology) is outside the hash and provokes no remote
  fetch at all. User decision, 2026-08-08.
- **[D4] No UI in this epic.** The observable surface is the log. A task-manager / admin-health view is
  Epic #5511's job, and this epic's `RefreshLog` extension is precisely what #5511 will render. User
  decision, 2026-08-08.
- **[D5] The log cleanup ships first, not last.** It is the measuring instrument: without a summary line
  reporting mode / scanned / changed / duration, no later slice can demonstrate — or falsify — that it
  got faster. It also settles the epic's secondary complaint ("quite noisy on update right now") before
  delta adds anything new to say.
- **[D6] `LastChangedRemote` (nullable UTC `DateTime`) is persisted per work item and per feature.**
  Additive, expand-only migration generated with the existing `CreateMigration` script. It must be
  copied explicitly inside `WorkItem.Update(…)`; the copy path drops members that are not plain settable
  properties, and a dropped timestamp silently degrades delta to "always refetch" — the failure is a
  performance regression with green tests, so it needs its own test.
- **[D7] Slice order: log → Jira Cloud → Jira Cloud portfolios → Jira DC → fingerprint → ADO →
  ServiceNow → Linear.** All connectors are *assessed* in this wave (matrix above). Whether slices 06-08
  stay in this epic or become a follow-on feature is an explicit checkpoint after slice 04, per the
  user's note ("once we have the jira cases, we can decide how to proceed with the others"). CSV is
  never in scope.
- **[D8] There is no partial mode.** An update is either `full` or `delta`. It is `full` when: the entity
  has never been swept, the fingerprint changed, any stored item lacks a `LastChangedRemote`, or the
  sweep itself failed. Anything ambiguous resolves to `full` — the expensive answer is always the safe
  one.
- **[D9] Only the remote fetch is deltaed.** Remaining-work rollup, feature extrapolation, percentile
  defaults and forecasts continue to recompute on every cycle exactly as today. They depend on
  wall-clock and on *other* teams' data, so skipping them because this team's items did not move would
  be wrong.
- **[D10] Time-driven derivations are evaluated for every stored item, not only the fetched ones.**
  Staleness is the case that bites: an item that stops changing is exactly the item that goes stale, and
  under delta it is exactly the item that stops being fetched. `AddStalenessEventIfThresholdCrossed`
  moves off the fetched-item loop and onto the stored set. This also preserves ADR-027's assumption that
  a re-sync re-derives the signals.
- **[D11] CSV is permanently out of scope.** Its fetch is a file the user already uploaded; there is no
  remote call to save. Stated so no later slice re-opens it.
- **[D12] Change detection compares per-item timestamps, never a global watermark.** Because the sweep
  returns a timestamp for *every* id, delta is `sweep.updated != stored.LastChangedRemote` per item.
  This removes clock skew, server-time drift and "what was `lastSync` exactly" from the design entirely.
  Residual risk — a second change landing inside the same timestamp granularity as the fetch — is
  bounded by treating any item whose remote timestamp falls inside the current sweep's uncertainty
  window as changed on the following cycle too. One extra fetch for a handful of items, never a missed
  change.

### Open questions

- **[OQ-1] Is the Jira DC identity sweep stable across back-to-back calls?** DC offset pagination over an
  unordered JQL is the documented source of duplicate `ReferenceId`s (ci-learnings 2026-05-25, and
  `DeduplicateByReferenceId` exists because of it). If the id set is *unstable* — not merely duplicated —
  then `removed = stored − sweepIds` would delete live items on DC. Resolved by a timeboxed probe at the
  head of slice 04, before any DC code is written. This is the single question that can invalidate D1 on
  the epic's named instance.
- **[OQ-2] How many teams/portfolios does a typical instance carry?** Determines whether the per-entity
  Information logging in `UpdateAll` is a nuisance or the dominant log volume. Does not block slice 01
  (the fix is the same either way); does inform the KPI-5 target.

---

## Wave: DISCUSS / [REF] Scope Assessment

**Verdict: OVERSIZED — split confirmed by the user, 2026-08-08.**

| Oversize signal | Reading |
|---|---|
| >10 user stories | Borderline — 8 stories |
| >3 bounded contexts / modules | **Yes** — work-tracking connectors, sync/update pipeline, configuration, observability |
| Walking skeleton needs >5 integration points | No — the skeleton needs one (Jira Cloud) |
| Estimated effort >2 weeks | **Yes** if all six connectors land together |
| Multiple independent user outcomes that could ship separately | **Yes** — the log, each connector, and the fingerprint each ship value alone |

Three signals fired. Split into 8 elephant-carpaccio slices, each ≤1 day, each with its own brief under
`docs/feature/epic-5687-faster-updates/slices/`. Slices 06-08 carry an explicit re-scoping checkpoint
after slice 04 (D7).

---

## Wave: DISCUSS / [REF] WS Strategy

**Strategy B — thin end-to-end slice through the existing stack.** Slice 02 (Jira Cloud, team work
items) is the walking skeleton: it introduces the whole delta contract — sweep, per-item comparison,
payload-for-changed, unchanged removal semantics, time-driven derivations off the stored set — through
one connector on one entity type. Every later connector slice is that same contract applied to a
different transport, which is why the contract ships *with* value rather than as its own abstraction
slice (carpaccio taste test: "if every slice depends on a new abstraction, ship the abstraction first" —
here the abstraction and the first value arrive together, deliberately).

---

## Wave: DISCUSS / [REF] Driving Ports

No new inbound surface. The existing ones are unchanged:

| Port | Change |
|---|---|
| Background `UpdateServiceBase` timer loop | None to the contract; emits the new summary log line |
| `POST api/v1\|latest/teams/{id}` / `.../portfolios/{id}` manual update triggers | None — a manual trigger runs whatever mode D8 resolves to |
| `GET api/v1\|latest/update/status` | None in this epic (Epic #5511 owns the richer view) |
| Container / systemd log stream (`docker logs`, `journalctl`) | **This is the observable surface** (D4) |

---

## Wave: DISCUSS / [REF] Pre-requisites

- A real Jira Cloud instance with ≥1000 issues in one team query.
- For slice 04: a Jira **Data Center** instance. Confirmed available (user, 2026-08-08) — a dev build
  will be run against a real DC system when the slice comes up. This makes OQ-1 answerable on real data
  rather than reasoned about, and it means the probe runs where the hazard actually lives. It is a
  *scheduled* dependency, not a blocker: slice 04 is the only slice that needs it, and the six slices
  either side of it do not.
- The dev instance on `:5169` with restored real recorded history for dogfooding the log line
  (`reference_dev_db_backup_restore`).
- `CreateMigration` PowerShell script for the D6 migration across all providers.
- No dependency on Epic #5511 — the relationship runs the other way (#5511 consumes what D6/slice 01
  records).

---

## Wave: DISCUSS / [REF] Out of Scope

- Any UI (D4). Explicitly deferred to Epic #5511.
- A manual "Force full refresh" action. D3 makes it unnecessary; adding it would make the automatic path
  look untrustworthy.
- Webhooks / push from the work-tracking system. A different epic and a different trust model; delta
  polling is what this one is about.
- Changing the refresh *interval* defaults. This epic makes a short interval affordable; whether the
  shipped default moves is a separate call once KPI-1 has real numbers.
- CSV (D11).
- Caching, in-memory or otherwise. Nothing in this epic adds a cache — the saving comes from not asking,
  not from remembering the answer.
- Write-back. `TriggerWriteBackForTeam` runs after the fetch and is untouched.

---

## Wave: DISCUSS / [REF] User Stories

Every story traces to a `job_id` in `docs/product/jobs.yaml`. Two stories are labelled
`@infrastructure`; neither constitutes a slice on its own (slice-composition gate).

---

### US-01 — One line per update that says what it did

`job_id: job-operator-read-an-update-log-that-says-something` · persona `platform-operator` · **slice 01**

As a platform operator, I want each completed update to log a single structured summary — which entity,
which mode, how many records were seen, how many were fetched, how long it took — and the per-entity
chatter demoted to Debug, so that reading the log tells me what the system is doing instead of what it
is iterating over.

#### Elevator Pitch
Before: an update writes 38-odd interleaved lines per entity, including "Updating Work Items for Team X"
three times and one "Checking last update" per team per cycle even when nothing updates, and nowhere
says what the update cost.
After: run `docker logs -f lighthouse` → sees `Update completed | Team 'Zenith' | mode=full |
scanned=4013 | fetched=4013 | 4m51s`, and one line per cycle instead of a page.
Decision enabled: whether this instance's refresh interval is affordable, and which team is the
expensive one — the two questions that decide whether to tune the interval at all.

**Acceptance criteria**
- AC-1.1 A completed team update emits exactly one Information-level summary line carrying entity type,
  entity name, mode, records seen, records fetched, duration and success.
- AC-1.2 A completed portfolio update emits the same line shape.
- AC-1.3 `mode` reads `full` for every update in this slice — the field exists before delta does, so the
  later slices change data, not format.
- AC-1.4 A cycle in which an entity is skipped (`ShouldUpdateEntity` false) emits **no** Information
  line for that entity. It stays available at Debug.
- AC-1.5 "Updating Work Items for Team {X}" appears at most once per update across
  `WorkItemService` and the connector.
- AC-1.6 Per-item and per-feature lines (`Added/Updated/Removed Work Item`, `Feature … Extrapolating`)
  are Debug or lower.
- AC-1.7 Information-level lines per entity per update ≤ 2 (KPI-5), asserted by a test that captures the
  logger.
- AC-1.8 `RefreshLog` gains the mode and the two counts; nothing already recorded is dropped or renamed.
- AC-1.9 A failed update still emits the summary line, with success=false, and still logs the error at
  Error level with its exception.

---

### US-02 — A Jira team refresh fetches only the issues that moved

`job_id: job-operator-sync-without-hammering-the-tracker` · persona `platform-operator` · **slice 02**

As a platform operator running Lighthouse against Jira Cloud, I want the second and later refreshes of a
team to download full issue payloads only for issues whose `updated` timestamp moved, while still
enumerating the whole query result so removals are caught, so that a routine cycle costs a cheap scan
instead of a full download plus changelog paging.

#### Elevator Pitch
Before: every cycle re-downloads all 4013 issues with their full field set and changelog, including the
paged changelog fetch for every long-lived issue — minutes of wall clock and thousands of requests, to
learn that 37 things changed.
After: run `docker logs -f lighthouse` → sees `Update completed | Team 'Zenith' | mode=delta |
scanned=4013 | fetched=37 | 11.8s`.
Decision enabled: the operator can shorten the refresh interval — the number in front of them is what
makes that safe to do, and what they show their Jira admin.

**Acceptance criteria**
- AC-2.1 The first update of a team (no stored `LastChangedRemote`) runs `mode=full` and stores a
  remote-changed timestamp for every item.
- AC-2.2 A subsequent update issues a sweep over the **same** query, requesting identity + `updated`
  only, and fetches full payloads only for ids whose timestamp differs from the stored one (D12).
- AC-2.3 An issue deleted, re-typed, or moved out of the query's states is removed from Lighthouse on
  the very next cycle — `removed = stored − sweepIds`, unchanged from today (D2).
- AC-2.4 An unchanged issue's stored fields, transitions and `CurrentStateEnteredAt` are **byte-identical**
  before and after a delta cycle.
- AC-2.5 An item that has not changed for longer than the team's staleness threshold still raises
  `WorkItemBecameStale` under delta — the staleness evaluation runs over the stored set, not the fetched
  set (D10). This AC fails on any implementation that leaves the evaluation on the fetch loop.
- AC-2.6 A sweep failure falls back to a full fetch and logs it; it never produces a partial result (D8).
- AC-2.7 `LastChangedRemote` survives `WorkItem.Update(…)` — asserted directly, because losing it
  degrades delta to full with every other test still green (D6).
- AC-2.8 Against a real Jira Cloud project with ≥1000 issues and ≤5% churn, a delta cycle issues ≤10% of
  the remote requests a full cycle issues (KPI-2), read from the summary line.
- AC-2.9 Remaining-work rollup and forecast triggering fire on a delta cycle exactly as on a full one (D9).

---

### US-03 — A Jira portfolio refresh fetches only the features that moved

`job_id: job-operator-sync-without-hammering-the-tracker` · persona `platform-operator` · **slice 03**

As a platform operator, I want the same delta treatment for a portfolio's Features and their parent
Features, so that portfolio updates — which fetch a second and third time for parents — stop being the
remaining expensive half of every cycle.

#### Elevator Pitch
Before: `mode=delta` on the team, and a full Feature + parent-Feature download on every portfolio cycle
right behind it — half the saving, undone.
After: run `docker logs -f lighthouse` → sees `Update completed | Portfolio 'Zenith Q3' | mode=delta |
scanned=214 | fetched=6 | 3.1s`.
Decision enabled: whether portfolio and team refresh intervals can be set to the same short value, or
whether portfolios still need to be throttled separately.

**Acceptance criteria**
- AC-3.1 `RefreshFeatures` and `RefreshParentFeatures` both run the two-phase contract from US-02.
- AC-3.2 A Feature removed from the query is removed from the portfolio on the next cycle (D2).
- AC-3.3 Feature state transitions and blocked transitions for unchanged Features are untouched.
- AC-3.4 A Feature that changed in one portfolio but is shared with another is refetched once and applied
  to both.
- AC-3.5 Extrapolation, default-size percentile and remaining-work recompute every cycle regardless of
  mode (D9).
- AC-3.6 The portfolio summary line reports mode and both counts (US-01 format).

---

### US-04 — The 25-year Data Center instance gets the same treatment, safely

`job_id: job-operator-sync-without-hammering-the-tracker` · persona `platform-operator` · **slice 04**

As a platform operator running an on-premise Jira Data Center instance with decades of history, I want
delta refreshes there too, and I want to know the identity sweep is trustworthy on DC's pagination before
it is allowed to drive deletions, so that the instance this epic was written for actually gets faster
without losing items.

#### Elevator Pitch
Before: the instance the epic names by name — on-prem DC, 25 years of history — takes minutes per cycle,
and its pagination is the one already known to hand back duplicate issues.
After: run `docker logs -f lighthouse` → sees `Update completed | Team 'Legacy Platform' | mode=delta |
scanned=18402 | fetched=54 | 22.4s`, where the previous full cycle read `mode=full | 6m12s`.
Decision enabled: whether this instance can be refreshed hourly rather than nightly — the question that
made someone file the epic.

**Acceptance criteria**
- AC-4.1 **Pre-slice probe (OQ-1)**: two back-to-back identity sweeps against a real DC instance return
  the same id *set*. Result recorded in the slice brief before implementation starts.
- AC-4.2 If the probe shows an unstable set, DC delta is **not** shipped on an unstable sweep; the slice
  records the finding and either adds a deterministic ordering clause to the sweep query or stops. A
  sweep that can lose an id must never drive `removed = stored − sweepIds`.
- AC-4.3 Duplicate ids within a DC sweep are collapsed the way `DeduplicateByReferenceId` already
  collapses duplicate issues, and the collapse is logged with the count.
- AC-4.4 US-02's AC-2.1 … AC-2.7 hold identically on the DC transport.
- AC-4.5 The DC and Cloud sweeps share one contract; a connector implements a sweep, not a strategy.

---

### US-05 — Changing a setting costs a refetch only when it changes what is fetched

`job_id: job-config-admin-know-which-settings-cost-a-refetch` · persona `config-admin` · **slice 05**

As a configuration administrator, I want Lighthouse to decide by itself whether my edit changed what gets
fetched, so that widening a query really does re-pull everything, and adding a wait state or tweaking a
blocked rule costs nothing at all.

#### Elevator Pitch
Before: nothing distinguishes the two kinds of edit, because every cycle refetches everything anyway —
so once delta exists, a query change would silently keep serving the old result set.
After: change the query in Team settings, save, and the next cycle logs `mode=full |
reason=configuration-changed`; add a wait state instead, save, and the next cycle logs `mode=delta |
scanned=4013 | fetched=0`.
Decision enabled: the admin can tune wait states, blocked rules and staleness thresholds freely, and
knows that a query edit really did take effect rather than hoping it did.

**Acceptance criteria**
- AC-5.1 A stored fetch fingerprint covers exactly: `DataRetrievalValue`, `WorkItemTypes`, `AllStates`,
  `DoneItemsCutoffDays`, additional field definitions, parent-override field,
  `WorkTrackingSystemConnectionId`.
- AC-5.2 Changing any one of them makes the next cycle `mode=full`, and the summary line names
  configuration as the reason.
- AC-5.3 Changing a wait state, blocked rule, staleness threshold, named cycle time, ordering policy or
  terminology leaves the next cycle at `mode=delta` and issues **zero** payload fetches when nothing
  moved remotely (KPI-4).
- AC-5.4 A guard test enumerates the fetch-shaping properties reachable from `PrepareQuery` and the
  connector call sites and fails when one is neither in the fingerprint nor on an explicit, commented
  exclusion list. Without this, the fingerprint drifts silently and delta serves stale data — the failure
  mode is wrong numbers with green tests.
- AC-5.5 An instance upgrading into this feature has no stored fingerprint, so its first cycle is
  `mode=full` (D8).
- AC-5.6 The fingerprint is stored per Team and per Portfolio independently.

---

### US-06 — Azure DevOps stops re-reading every revision

`job_id: job-operator-sync-without-hammering-the-tracker` · persona `platform-operator` · **slice 06**

As a platform operator on Azure DevOps, I want the same two-phase fetch, so that the per-item revision
history read — the dominant cost on ADO — only happens for items that actually changed.

#### Elevator Pitch
Before: the WIQL is already cheap, but every returned id then costs a `GetWorkItemsAsync` slot **and** a
`GetRevisionsAsync` round trip to rebuild transitions, on every cycle, for every item.
After: run `docker logs -f lighthouse` → sees `Update completed | Team 'Lighthouse' | mode=delta |
scanned=1204 | fetched=9 | 4.6s`.
Decision enabled: same as US-02 — whether the interval can be shortened — and it is the connector the
maintainer dogfoods, so it is where the number is checked first.

**Acceptance criteria**
- AC-6.1 The identity sweep is the existing WIQL (already ids only); the changed set comes from a second
  WIQL with `AND [System.ChangedDate] >= @lastSweep`, and the two are combined per D12.
- AC-6.2 `GetRevisionsAsync` is called only for ids in the changed set.
- AC-6.3 A transition that happened on an unchanged item is impossible by construction, and a test
  asserts an item whose `System.ChangedDate` moved always has its revisions re-read.
- AC-6.4 US-02's AC-2.3 … AC-2.7 hold on ADO.
- AC-6.5 Portfolio Features and parent Features follow the same path.

---

### US-07 — ServiceNow and Linear join the contract

`job_id: job-operator-sync-without-hammering-the-tracker` · persona `platform-operator` · **slices 07, 08**

As a platform operator on ServiceNow or Linear, I want delta refreshes there too, so that no connector is
left as the slow one.

#### Elevator Pitch
Before: two connectors still re-read every record and every state span / history connection each cycle.
After: run `docker logs -f lighthouse` → sees `mode=delta | scanned=880 | fetched=12` for a ServiceNow
team and the same shape for a Linear team.
Decision enabled: whether the instance's refresh interval can be a single global choice rather than one
per connector.

**Acceptance criteria**
- AC-7.1 ServiceNow sweeps on `sys_updated_on` with `sysparm_fields` narrowed to identity + timestamp,
  keeping `InAStableOrder` so the existing paging guard still holds.
- AC-7.2 Per-record state-span reads (`ReadSpans` / `ReadHistory`) happen only for changed records.
- AC-7.3 Linear sweeps on `updatedAt`, and the history connection fragment is requested only for changed
  issues/projects.
- AC-7.4 US-02's AC-2.3 … AC-2.7 hold on both.
- AC-7.5 The existing `paging_repeated_records` guard still fires on a genuinely repeated `sys_id`.

---

### US-08 — `LastChangedRemote` is persisted `@infrastructure`

`job_id: job-operator-sync-without-hammering-the-tracker` · persona `platform-operator`

Additive, expand-only migration adding a nullable UTC `LastChangedRemote` to work items and features, and
a fetch-fingerprint column to Team and Portfolio. **Labelled `@infrastructure`; lands as a precursor
commit inside slices 02 and 05 respectively, never as a slice of its own** (slice-composition gate).

---

## Wave: DISCUSS / [REF] Story Map

**Backbone** (left to right, as the operator experiences it):

| Understand what an update costs | Fetch less on Jira | Keep configuration honest | Cover the rest of the connectors |
|---|---|---|---|
| US-01 summary line, quiet logs | US-02 Jira Cloud team | US-05 fetch fingerprint | US-06 Azure DevOps |
| | US-03 Jira Cloud portfolio | | US-07 ServiceNow, Linear |
| | US-04 Jira Data Center | | |

**Walking skeleton**: US-02 (slice 02). Thinnest path that proves the whole contract end to end.

**Slices** — briefs at `docs/feature/epic-5687-faster-updates/slices/`:

| # | Slice | Story | ADO | Est. | Learning hypothesis (disproved if it fails) |
|---|---|---|---|---|---|
| 01 | Update log signal | US-01 | #5724 | ~4h | That an update even *knows* what it fetched. If the summary cannot be produced from data at hand, delta cannot be reported or measured. |
| 02 | Jira Cloud team delta | US-02 | #5725 | ~6h | That a cheap identity sweep is materially cheaper than the full fetch. If Jira charges the same for the scan, the epic's premise is wrong. |
| 03 | Jira Cloud portfolio delta | US-03 | #5726 | ~5h | That the contract generalises from work items to Features without a second design. |
| 04 | Jira Data Center delta | US-04 | #5727 | ~6h + 2h probe | That DC's pagination yields a *stable* id set. If not, delta cannot drive deletion on the epic's named instance. |
| 05 | Fetch fingerprint | US-05 | #5728 | ~6h | That the fetch-shaping property set can be enumerated in one place. If it cannot, config invalidation cannot be automatic and D3 collapses into "any save refetches". |
| 06 | Azure DevOps delta | US-06 | #5729 | ~6h | That `System.ChangedDate` aligns with revision history. If an item's revisions can move without `ChangedDate` moving, ADO delta drops transitions. |
| 07 | ServiceNow delta | US-07 | #5730 | ~5h | That narrowing `sysparm_fields` does not disturb the existing paging guard. |
| 08 | Linear delta | US-07 | #5731 | ~4h | That the history fragment is separable from the issue query. |

**Checkpoint after slice 04** (D7): decide whether 06-08 stay in this epic or become a follow-on feature.

### Carpaccio taste tests

| Test | Result |
|---|---|
| Any slice shipping 4+ new components? | No. Largest is slice 02: sweep method, per-item comparison, persisted timestamp. |
| Every slice depends on a new abstraction? | Slices 03-08 depend on the delta contract — which slice 02 ships **together with** its own user value, deliberately, rather than as a bare abstraction slice. Documented, accepted. |
| Does any slice disprove a pre-commitment? | Yes, each — see the table. Slice 04's probe can invalidate D1 on DC; slice 05's guard test can invalidate D3. |
| Synthetic data only? | No. Slices 02-04 require a real Jira instance; slice 01 dogfoods the `:5169` instance with real recorded history. |
| Two slices identical except for scale? | No. 02/03 differ by entity type and code path; 02/04 by transport and failure mode; 06/07/08 by API. |
| Slice with only `@infrastructure` stories? | No. US-08 is a precursor commit inside slices 02 and 05, never a slice. |

---

## Wave: DISCUSS / [REF] Prioritization

1. **Slice 01 first** because it is the instrument, not because it is easy. Every later slice's
   acceptance criteria are read off the line it introduces, and it settles the epic's secondary
   complaint independently of whether delta ever ships.
2. **Slice 02 second** — highest learning leverage. If the sweep is not materially cheaper, the epic
   stops here having cost one day, not six.
3. **Slice 03 immediately after 02**, while the contract is fresh, because a half-delta cycle (team
   delta, portfolio full) shows only half the win and would make KPI-1 read as a failure.
4. **Slice 04 fourth** even though it is the named pain, because its probe is cheaper to run once the
   Cloud shape is proven — the probe then tests DC's *pagination*, not the design.
5. **Slice 05 fifth**, before the remaining connectors: once three connectors' worth of delta exists,
   a silent config-drift bug would be three bugs. It is a correctness gate on everything shipped so far.
6. **Slices 06-08 last**, in descending value (ADO's per-item revision read is the biggest remaining
   cost), subject to the D7 checkpoint.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| **KPI-1** Subsequent-update duration | Median delta-cycle duration ≥70% lower than the same entity's full-cycle baseline | `RefreshLog.DurationMs`, 10 consecutive cycles per mode, same team |
| **KPI-2** Remote request count | A delta cycle issues ≤10% of a full cycle's remote requests, on a ≥1000-item team with ≤5% churn | Request count on the US-01 summary line |
| **KPI-3** Removal correctness | For 20 consecutive delta cycles, stored ReferenceIds == sweep ids, zero drift | Integration test + a dogfood assertion on `:5169` |
| **KPI-4** Local-only edits cost nothing | A wait-state / blocked-rule / staleness edit produces **0** payload fetches on the next cycle | `fetched=0` on the summary line |
| **KPI-5** Log volume | ≤2 Information lines per entity per update; 0 for a skipped entity | Logger-capturing test (AC-1.7) + eyeball on `:5169` |
| **KPI-6** Staleness still fires | An item untouched past the threshold raises `WorkItemBecameStale` under delta | Acceptance test for AC-2.5 |

KPI-1 and KPI-2 are unmeasurable before slice 01 ships — which is the argument for D5.

---

## Wave: DISCUSS / [REF] Definition of Done

1. All acceptance criteria for the slice pass as automated tests.
2. `dotnet build` zero warnings; `dotnet test` green.
3. `pnpm test`, `pnpm build` (zero warnings), Biome clean — N/A for slices with no frontend change, stated explicitly per slice.
4. Mutation testing ≥80% kill rate on the changed backend surface (`per-feature` strategy).
5. SonarQube Cloud: no new issues of any severity.
6. EF migration generated with `CreateMigration` across all providers, additive only.
7. Docs updated per-feature — the update/refresh concept page states what delta means for freshness, using seeded terminology.
8. ADO story transitioned; slice pushed only after CI is green.
9. The slice's learning hypothesis has an explicit verdict recorded in its brief — confirmed or disproved, never blank.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Verdict | Evidence |
|---|---|---|---|
| 1 | Business value articulated | ✅ | Epic #5687 description + KPI-1/KPI-2; the named on-prem DC instance is the concrete case |
| 2 | Job traceability | ✅ | 3 jobs written to `docs/product/jobs.yaml`; every story carries a real `job_id` (US-08 is `@infrastructure` and is a precursor commit, not a slice) |
| 3 | Acceptance criteria testable | ✅ | 44 ACs, each observable from a log line, a stored value, or a request count |
| 4 | Dependencies identified | ✅ | Real Jira Cloud instance; a real Jira DC system for slice 04, confirmed available via a dev build (user, 2026-08-08); `:5169` dev instance; `CreateMigration`. No dependency on Epic #5511 |
| 5 | Sliced ≤1 day each | ✅ | 8 briefs, 4-6h each, taste tests passed |
| 6 | No known blockers | ✅ | None. Slice 04's Jira Data Center access is arranged (dev build against a real system, user 2026-08-08), so OQ-1 is scheduled rather than blocked |
| 7 | Observable surface defined | ✅ | D4 — the log is the surface this epic ships; UI deferred to #5511 with the user's explicit decision |
| 8 | Test data / environment available | ✅ | `:5169` with real recorded history; demo data for the non-Jira connectors; ServiceNow PDI already used by epic-5513 |
| 9 | Outcome KPI with numeric target | ✅ | 6 KPIs, each with a number and a named measurement source |

**Requirements completeness: 0.98.** The remaining 0.02 is OQ-1 itself: the DC pagination question is
answerable — access is arranged — but not yet answered, and slice 04 is written to stop and record rather
than guess if the probe fails.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key decisions
See Locked Decisions above (D1-D12). The four that shape everything downstream:
- **[D1/D2]** Two-phase sweep, so removal semantics survive untouched.
- **[D3]** Fetch fingerprint, so config invalidation is automatic and narrow.
- **[D10]** Time-driven derivations move off the fetch loop — the trap that would otherwise ship silently.
- **[D4/D5]** No UI; the log is both the deliverable and the instrument, and it ships first.

### Requirements summary
Primary jobs: refresh a decades-deep tracker without refetching it whole; know which settings cost a
refetch and which cost nothing; read a log that says what an update did. Feature type: backend, with a
cross-cutting observability edge. Walking skeleton: slice 02, Jira Cloud team work items.

### Constraints established
- Removal detection must stay exact — a sweep that can lose an id may not drive deletion (D2, AC-4.2).
- Ambiguity resolves to a full fetch, always (D8).
- Only the remote fetch is deltaed; derived work still recomputes each cycle (D9).
- Migrations additive / expand-only (D6, project standing rule).
- CSV never participates (D11).

### Upstream changes
None. No DISCOVER or DIVERGE artifacts exist for this feature, so no prior assumption was revised.

---

## Wave: DISCUSS / [WHY] Alternatives Considered

Rendered on request (`ask-intelligent` trigger: *cross-context complexity* — 4 bounded contexts, 4
connector technologies). What was weighed and rejected behind each locked decision, and what would have
to be true for the rejected option to win.

### D1 — the delta shape

| Option | Why rejected | Would win if… |
|---|---|---|
| **Delta query + periodic full reconcile.** Query becomes `updated >= lastSync`; a full sweep runs every Nth cycle or nightly to catch removals. | Turns correctness into a scheduling parameter. An item that leaves the query lingers for up to N cycles, and every forecast built on it is wrong for that window — invisibly, because the sync reports success. The reconcile interval then becomes a knob nobody can set correctly: short enough to be safe defeats the point, long enough to be cheap is unsafe. | The identity sweep turned out to cost nearly as much as the full fetch. Then the only saving left would come from not enumerating at all, and the reconcile cadence would be the price of that. Slice 02's learning hypothesis is exactly this question. |
| **Delta query + manual "Full refresh" button.** No automatic reconcile; an admin forces a full pull when they suspect drift. | Makes correctness a user chore, and the button's existence is an admission that the automatic path is not trusted. It also fails silently in the common case — nobody clicks a button for a problem they cannot see. | Never, on its own. If the DC probe (OQ-1) fails, a *forced* full fetch on a schedule may be the DC-specific fallback — but as a fallback for one transport, not as the design. |
| **Two-phase sweep (chosen).** Same query, identity + timestamp first, payload for the changed only. | — | — |

The decisive argument is that D2 falls straight out of it: because the sweep enumerates the full result
set every cycle, the removal rule needs no new reasoning at all. The cheaper options both require a
*new* argument for why deletion is still correct, and neither has one that survives "how long is the
window, and what is wrong during it?".

### D3 — configuration invalidation

| Option | Why rejected | Would win if… |
|---|---|---|
| **Any settings save invalidates.** Simple, always safe. | Wastes the win on precisely the case the epic calls out by name: adding a wait state would force a full 25-year refetch. It is also self-defeating — an admin who learns that every save is expensive stops tuning, which is worse than the original problem. | The fetch-shaping property set proves un-enumerable (slice 05's learning hypothesis). Then this is the honest fallback, and the epic loses one of its two promises rather than shipping a silent staleness bug. |
| **Explicit admin choice on save** ("refetch everything? [Y/N]"). | Puts a correctness decision in front of a user who has no way to answer it. The right answer is derivable from the diff; asking is an abdication dressed as control. | Never in this design. It would only make sense if fetch-shaping-ness were genuinely ambiguous — and if it were, the guard test could not exist. |
| **Fetch fingerprint (chosen).** | — | — |
| Per-field granularity ("only the states changed, so pull only the newly included states") | Considered and dropped before it reached the option list. It multiplies the correctness surface by the number of fetch-shaping fields, for a saving that only appears on the rare edit. A fingerprint is a boolean deliberately. | The full refetch after a query edit turned out to be unacceptably expensive on the largest instances *and* frequent. Neither is currently believed. |

### D4 — no UI

The alternative was the obvious one: put mode / scanned / fetched / duration on a screen. Rejected
because ADO Epic #5511 "Task Manager" already owns that surface — running and queued updates, cancel,
connection health, an admin dashboard — and building a narrower version here would either be thrown away
or become the thing #5511 has to work around. The `RefreshLog` fields added in slice 01 are the same
fields #5511 will render, so this epic feeds it rather than competing with it. Cost accepted with eyes
open: until #5511 lands, the only way to see the saving is the log, which means a self-hoster sees it and
a SaaS tenant does not.

### D5 — log first, not last

The alternative order — ship delta, then clean the logs — is the intuitive one, and it is wrong for a
specific reason: KPI-1, KPI-2 and KPI-5 are all read off the summary line. Shipping delta first means
shipping it unmeasured, and "it feels faster" is not a verdict a learning hypothesis can be closed with.
The secondary argument is that the log cleanup has standalone value: if the epic stopped after slice 01,
the user's own complaint ("quite noisy on update right now") would still be paid.

### D7 — connector order

Rejected: **ADO first**, on the grounds that it is structurally the smallest change (the WIQL is already
an identity sweep). It would prove the contract most cheaply — and prove it somewhere the pain does not
live. The epic exists because of a Jira DC instance; validating the design on the connector that hurts
least risks a confident contract that turns out not to apply where it matters.

Rejected: **all connectors in one slice.** Four auth models, four pagination models, four history models
— fails the carpaccio taste test outright ("ships 4+ new components").

Also weighed: **DC before Cloud**, since DC is the named pain and carries the higher uncertainty
(pagination stability). Cloud-first won because the probe is then testing DC's *pagination* rather than
the *design* — two unknowns separated instead of confounded. If the design is wrong, Cloud finds it in
six hours without a DC instance in the loop.

### D9/D10 — what stays whole

The tempting optimisation was to skip derived work when nothing changed: no fetch, no rollup, no
forecast. Rejected because both are wall-clock-driven or cross-entity:

- **Remaining work and extrapolation** read *other* teams' items via `ParentReferenceId`, and the
  percentile default size reads a rolling history window. "This team's records did not move" says
  nothing about either.
- **Staleness** is the pure case: it is a function of elapsed time against `CurrentStateEnteredAt`. An
  item that stops changing is exactly the item that goes stale, so the optimisation would delete the
  signal it is supposed to preserve.

This is also why ADR-027's guarantee — the dispatcher is transport-only, recovered on the next re-sync —
survives: the re-sync still re-derives everything, it just stops re-downloading what it already has.

### D12 — per-record comparison, not a watermark

The conventional design is a stored `lastSyncedAt` per entity, compared against the remote timestamp.
Rejected because it imports three problems that the per-record comparison simply does not have: clock
skew between Lighthouse and the tracker, ambiguity about whether the watermark means "when the sweep
started" or "when it finished", and the retry question (does a failed cycle advance it?). Since the sweep
already returns a timestamp per record, comparing per record costs nothing extra and removes all three.

The residual — a second change landing inside the same timestamp granularity as the fetch — is real and
bounded, and the mitigation (re-fetch anything inside the sweep's uncertainty window on the next cycle
too) costs a handful of extra payloads rather than a missed change. A watermark design has the same
residual *plus* the three problems.

### D11 — CSV

No alternative was weighed; it is recorded as a decision only so a later slice does not re-open it. The
"fetch" is a file the user uploaded. There is no remote call to save, so delta has nothing to act on.

---

## Wave: DISCUSS / [WHY] Persona Narrative

Rendered on request (`ask-intelligent` trigger: *multi-stakeholder* — 3 personas across the stories).
Extended profiles as they apply to *this* feature; the canonical persona files stay in
`docs/product/personas/`.

### platform-operator — primary

The self-hoster flavour dominates here, not the LPW SaaS-operator one. This is the person who put
Lighthouse in a container next to their own Jira, wired it to a query someone else in the org owns, and
now has a relationship with the platform team that they would prefer stayed quiet.

**Goals in this feature**
- Run a refresh interval chosen for how fresh the numbers should be, not for what the fetch costs.
- Be able to answer "what does Lighthouse cost us?" with a number, in a channel, without a dashboard.
- Never be the reason a shared tracker got slow.
- Keep the diagnostics they debug with — quieter is only better if nothing is lost.

**Frustrations**
- A refresh takes minutes, so the interval got widened to an hour, so the numbers are stale, so people
  trust them less — a chain that starts at fetch cost and ends at adoption.
- The two facts they want (how long, how much) are recorded to `RefreshLog` and never surfaced.
- The log narrates the loop instead of the outcome: one "Checking last update" per entity per cycle
  *before* deciding not to update it, and "Updating Work Items for Team X" three times per update.
- The oldest, longest-lived issues — the ones that never change — are the most expensive to re-read,
  because that is where the changelog depth is. The cost scales with the org's history, not its activity.

**Mental model**
- A sync is a conversation with someone else's system, and Lighthouse is a guest in it.
- "It saw everything" and "it downloaded everything" are different claims, and only the first one is
  needed for correctness. This is the insight the whole design rests on, and it is why the summary line
  reports *scanned* and *fetched* as two numbers rather than one.
- Freshness is a dial. Cost should not be what sets it.
- Silence in a log is information: if an entity did not need updating, the right output is nothing.

**Vocabulary for this feature**
- "sweep" — the cheap pass that asks only which records exist and when each last changed.
- "full" / "delta" — the two update modes. There is no third; anything ambiguous is full.
- "scanned vs fetched" — how many records the sweep saw, versus how many were downloaded in full. A
  healthy cycle has a large first number and a small second one.
- "churn" — the fraction of a query's records that move between cycles. Low churn is what makes delta
  worth having; an instance at 100% churn gets nothing from this epic and should not be surprised.

### config-admin — secondary, and the one carrying the anxiety

Often the same human as the operator wearing a different hat, but the act that makes them this persona is
opening a settings form and saving it.

**Goals in this feature**
- Tune wait states, blocked rules, staleness thresholds and named cycle times as often as the team's
  understanding changes, without a cost narrative attached to each save.
- Know that a query change actually took effect, rather than hoping it did.

**Frustrations**
- Today there is no distinction between an edit that changes what is fetched and one that does not,
  because everything is refetched anyway — so the distinction has never had to exist, and the moment
  delta ships, its absence becomes a silent staleness bug rather than a missing convenience.

**Mental model**
- Settings fall into two kinds: ones that describe *what to go and get*, and ones that describe *what to
  make of what we already have*. The second kind should be free.
- The system should classify the edit; being asked "refetch everything? [Y/N]" on save would be the tool
  handing back a question it is better placed to answer.
- Order does not matter: re-saving the same states in a different sequence is not a change.

**Vocabulary for this feature**
- "fetch-shaping" — a setting that changes the remote query: the query text, work item types, states, the
  done-items cutoff, additional fields, the parent-override field, the connection.
- "local-only" — everything else. Wait states, blocked rules, staleness threshold, named cycle times,
  ordering policy, terminology.
- "fingerprint" — the stored hash of the fetch-shaping set; a mismatch is what makes the next cycle full.

### flow-coach — affected, never acting

The flow coach never appears in a story in this epic and never touches a setting in it. They are here for
one reason: they are who is harmed if delta is wrong, and naming them is what keeps the correctness ACs
from reading as paranoia.

**What they never notice if the epic succeeds**
- Time-in-state, aging pace, cumulative state time and blocked history stay identical across a delta
  cycle — AC-2.4's byte-identical assertion exists for them.
- An item that has sat untouched past the team's staleness threshold still shows up as stale — AC-2.5,
  and the reason D10 exists at all.
- An item that was deleted or moved out of the query disappears on the next cycle, not eventually —
  AC-2.3.

**What they would see if it went wrong**: nothing dramatic. Numbers that are quietly a little off, a
stale item that never surfaces, a chart with a phantom row. No error, no failed sync, no log line. That
failure shape — wrong data with a green pipeline — is why three of this epic's acceptance criteria are
about things *not* changing.

---

## Wave: DISCUSS / [REF] Telemetry

`DocumentationDensityEvent` emission is **not** recorded for this wave: the nWave helper
(`scripts/shared/telemetry.py:write_density_event`) is not present in this installation, and the wave
contract forbids writing the JSONL directly. Stated rather than skipped. Two expansions
(`alternatives-considered`, `persona-narrative`) were triggered and accepted; that is the fact the events
would have carried.

---

## Handoff

**To**: `nw-solution-architect` (DESIGN) — full artifact set · `nw-platform-architect` (DEVOPS) — the
Outcome KPIs section only.

DESIGN's first questions are already framed: where the sweep belongs on `IWorkTrackingConnector` (a new
capability method versus widening `GetWorkItemsForTeam`), whether the fingerprint lives on the entity or
in a side table, and where the staleness evaluation moves to once it leaves the fetch loop (D10).
