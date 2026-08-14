# Feature Delta — epic-4365-dependencies

**ADO**: Epic #4365 "Dependencies" (New, created 2026-02-23, tagged `Premium`, `Documentation`,
`Release Notes`) · **Feature type**: cross-cutting (work-tracking connectors + forecasting engine +
Feature list UI + licensing) · **Density**: lean · **DISCUSS run**: 2026-08-14

The epic asks for one thing in one line: *"Set dependencies on Features, then in the forecast, 'jump'
over them until the dependent Features are forecasted to be done."* Reading the forecasting engine
during this wave turned that line into something more interesting than it looks, and turned two of its
sub-bullets into decisions the sketch could not have anticipated.

1. **"Jump over" is not a date shift, and that is where the value is.**
   `ForecastService.GetSimulationResultsOfFeatureToUpdate` (`:201-209`) draws each simulated day's
   throughput from the first `min(FeatureWIP, remaining)` Features that still have remaining work, in
   order. Skipping a waiting Feature does not merely push *its* dates out — it hands its capacity to the
   Features **below** it, which finish **earlier**. A naive `max(own date, blocker's date)` gets the
   waiting Feature roughly right and every other Feature wrong. The mechanic has to live inside the
   simulation loop, per trial, or it is not this feature.

2. **The simulation is per-team and independent, so cross-team dependencies do not fit today.**
   `RunMonteCarloSimulation` (`:108-131`) groups `simulationResults` by team and runs each group's 10,000
   trials in its own `Task.Run`, each with its own day counter. Inside one trial of team T, no other
   team's Features exist and no other team's completion day is knowable. Since a dependency between two
   Features in one Portfolio very often crosses teams, the epic's core promise is unreachable without
   restructuring that loop. **The user's decision (2026-08-14) is to restructure it** — see D3.

3. **The surface this lands on already exists, and was built for it.** Epic #5375 (Manual Sorting)
   shipped `/features` (`FeaturesView.tsx`) as a general Feature view, and its D17 says in as many
   words that it is *"the surface that will later host ADO Epic #4365 'Dependencies'"*. The warnings
   column the epic asks for is `WarningsIndicator.tsx`, already rendering two warning kinds through the
   shared `FeatureListDataGrid`/`columns.tsx` factory. This epic writes a column and a dialog, not a page.

4. **"Blocked" is already taken.** Epic #5074 shipped blocked items — `WorkItem.IsBlocked`,
   `BlockedSince`, blocked-history widgets — and `blocked` is a **renameable terminology key**
   (`TerminologyKeys.ts:18`). A Feature row that says "blocked" for two unrelated reasons is unreadable.
   This feature says *depends on* and never *blocked by*, whatever the tracker calls its field (D10).

5. **The relation payloads are three different shapes, and only three connectors have one.** ADO already
   fetches `WorkItemExpand.Relations` on the parent path
   (`AzureDevOpsWorkTrackingConnector.cs:1043`) — the extension point is open, but the relation URL
   carries an id and no title, so names cost a follow-up request. Jira sends an explicit `fields=` list
   (`:1613`), so `issuelinks` must be added to it, and returns the summary inline. Linear is GraphQL and
   returns titles inline for free. **ServiceNow and CSV have no standard dependency field at all** —
   they are out of scope, not deferred-with-a-plan.

6. **Ordering is solved and must not be re-solved.** `FeatureRepository.GetAll()` orders through
   `IFeatureOrdering`, which switches between `ManualRankComparer` and `FeatureComparer` at one point.
   This feature reads that order and never writes it. A dependency whose blocker is ranked *below* its
   dependent is a warning, not an auto-reorder (D12).

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ⊘ `docs/feature/epic-4365-dependencies/discover/` (not found — no DISCOVER wave ran)
- ⊘ `docs/feature/epic-4365-dependencies/diverge/` (not found — no DIVERGE wave ran)
- ✓ `docs/product/jobs.yaml` (schema_version 1, 98 jobs) — none covers dependencies between Features.
  Nearest neighbours are the four `epic-5375-manual-sorting` ordering jobs, which establish that the
  sequence the forecast consumes is a first-class user concern; this epic adds the constraint that
  sequence cannot express.
- ✓ `docs/product/journeys/epic-5375-manual-sorting.yaml` — read in full. Its D17, D10, D12 and D16 are
  load-bearing here and are carried forward rather than re-decided.
- ✓ `docs/product/journeys/` (41 journeys) — none touches dependencies.
- ✓ `docs/product/personas/` (9 personas) — `product-owner`, `delivery-forecaster` and
  `delivery-lead-rte` are reused verbatim. No new persona needed.
- ✓ `docs/product/kpi-contracts.yaml` — the `measurement_scope` convention
  (`per_instance` / `vendor_demo_only` / `opt_in_telemetry_required`) is inherited by every KPI below.
- ⊘ `docs/product/vision.md`, `docs/project-brief.md`, `docs/stakeholders.yaml` (not found — product
  SSOT lives under `docs/product/` in this repo)
- ✓ `CLAUDE.md`, `docs/ci-learnings.md` — standing rules applied (expand-only migrations via
  `CreateMigration`, quality gates, per-feature docs, configurable terminology, no internal references
  in comments).
- ✓ **Code read during this wave**: `Services/Implementation/Forecast/ForecastService.cs`,
  `Models/Feature.cs`, `Models/WorkItemBase.cs`, `Models/FeatureComparer.cs`,
  `Models/ManualRankComparer.cs`, `Models/FeatureOrderKey.cs`,
  `Services/Implementation/FeatureOrdering.cs`,
  `Services/Implementation/Repositories/FeatureRepository.cs`, `API/DTO/FeatureDto.cs`,
  `API/FeaturesController.cs`, `Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs`,
  `WorkTrackingConnectors/AzureDevOps/AzureDevOpsWorkTrackingConnector.cs` + `WorkItemExtensions.cs`,
  `WorkTrackingConnectors/Jira/JiraWorkTrackingConnector.cs`,
  `WorkTrackingConnectors/Linear/LinearWorkTrackingConnector.cs`,
  `Models/WorkTrackingSystemOptionsOwner.cs`, `pages/Features/FeaturesView.tsx`,
  `components/Common/FeatureListDataGrid/columns.tsx` + `WarningsIndicator.tsx`,
  `hooks/useLicenseRestrictions.ts`, `hooks/useFeatureOrdering.ts`, `models/TerminologyKeys.ts`.
- ✓ **ADO** #4365 including its 2026-05-24 comment, which supplies the exact per-tracker field shapes and
  is quoted rather than paraphrased in D14.

No DISCOVER evidence exists to contradict, so no contradiction check was possible and none is claimed.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this feature |
|---|---|
| `product-owner` | Primary. Owns the order the forecast runs in (epic #5375) and now sees the constraint that order cannot express. The person who has to explain why a Feature is late, and who reads the dependency dialog to find out. |
| `delivery-forecaster` | Consumes the dates. Does not author dependencies, but is the persona harmed most by their absence — the forecast is confidently wrong, with nothing on screen suggesting it might be. Also the free-tier persona who must be told the forecast is ignoring something. |
| `delivery-lead-rte` | Portfolio scope. Reads a Feature list to find where the chain breaks — a dependency pointing outside the Portfolio, a blocker ranked below its dependent, a loop. Wants the map, not the individual edge. |
| `config-admin` | Appears once, in slice 06, and only on instances whose tracker does not use standard dependency links. Owns the Portfolio setting that names which field carries them — the same person who already sets the parent override next to it. |

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-forecast-honours-what-cannot-start-yet` | When one Feature cannot start until another finishes, make the forecast simulate that, so the dates I present are not fiction. |
| `job-po-see-what-a-feature-is-waiting-on` | When a Feature looks stalled, show me what it is waiting on without my opening the work tracking system. |
| `job-config-admin-point-at-the-field-that-carries-dependencies` | When my teams record dependencies in a custom field rather than the tracker's built-in link, let me tell Lighthouse which field that is, once, for the whole Portfolio. |
| `job-lead-see-where-the-dependency-chain-breaks` | When a dependency cannot be honoured — it points outside the Portfolio, or its blocker is ranked below it, or it loops — tell me plainly rather than quietly doing something else. |
| `job-forecaster-know-the-forecast-is-ignoring-a-dependency` | When my instance is not licensed for dependency-aware forecasting, tell me the dependency exists and that these dates do not account for it. |
| `job-forecast-covers-dependencies-that-cross-teams` | When the Feature I am waiting on belongs to another team, make the forecast account for it too — most real dependencies cross a team boundary. |

Full job stories, dimensions, four forces and opportunity scores are written to
`docs/product/jobs.yaml`.

### Opportunity scores

| Job | Importance | Satisfaction | Gap | Note |
|---|---|---|---|---|
| `job-forecast-honours-what-cannot-start-yet` | 5 | 0 | **5** | The product's core output is computed as though every Feature could start today. There is no partial workaround: nothing in Lighthouse can express "not yet". |
| `job-forecast-covers-dependencies-that-cross-teams` | 5 | 0 | **5** | Same importance, and the same zero — but scored separately because it is the half that costs a simulation-engine change, and separating them is what lets the cheaper half ship first. |
| `job-config-admin-point-at-the-field-that-carries-dependencies` | 4 | 0 | **4** | Satisfaction 0 because the instance gets *nothing* — not a degraded version. Importance 4 rather than 5 because it is conditional on how a given tracker is configured; for the instances it applies to it is worth 5, for the rest it is worth nothing. |
| `job-forecaster-know-the-forecast-is-ignoring-a-dependency` | 4 | 0 | **4** | Today the free instance is not merely unlicensed, it is uninformed — the dates look exactly as authoritative as licensed ones. |
| `job-po-see-what-a-feature-is-waiting-on` | 4 | 1 | **3** | Partly satisfied: the answer exists, in the tracker, one context switch away. The cost is the switch and the fact that nobody makes it while reading a forecast. |
| `job-lead-see-where-the-dependency-chain-breaks` | 3 | 0 | **3** | Lower importance because it is a diagnostic rather than an outcome — but it is the job that stops the other five from being quietly wrong. |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

| Surface | Location | State today |
|---|---|---|
| Feature ordering | `FeatureOrdering.cs`, consumed by `FeatureRepository.GetAll:18` | One switch point between `ManualRankComparer` and `FeatureComparer`, tie-broken by `Id`. Shipped by epic #5375. This feature reads it and never writes it. |
| Simulation eligibility | `ForecastService.cs:201-209` | `simulationResults.Where(x => x.HasWorkRemaining)`, then a random pick within the first `min(FeatureWIP, count)`. **The single insertion point for D2.** |
| Simulation grouping | `ForecastService.cs:108-131` | `GroupBy(s => s.Team)`, one `Task.Run` per team, one independent day counter each. **The single thing D3 changes.** |
| Simulation seeding | `InitializeSimulationResults` (`:164-177`) | One `SimulationResult` per (Feature, Team) pair with `RemainingWorkItems > 0`. A done Feature already has no row, which is why D8's "already finished blocker" case costs nothing. |
| Feature model | `Models/Feature.cs` | Carries `ManualRank` (deliberately absent from `Update`, so sync cannot overwrite it) — the exact precedent D5 copies. No relation of any kind to another Feature. |
| Synced fields | `Models/WorkItemBase.cs:18-56` | `ParentReferenceId` is the only inter-item link, a single string, overwritten from source on every sync. |
| Connector port | `IWorkTrackingConnector.cs` | Ten methods, none returning relations. A new method is owed. |
| ADO relations | `AzureDevOpsWorkTrackingConnector.cs:1043` | Already requests `WorkItemExpand.Relations` for the parent path; `WorkItemExtensions.cs:25-27` already walks `workItem.Relations`. Relation URLs carry an id, not a title. |
| Jira fields | `JiraWorkTrackingConnector.cs:1560`, `:1613` | Explicit `fields=` list per request. `issuelinks` is not in it. |
| Linear query | `LinearWorkTrackingConnector.cs:660-726` | Hand-built GraphQL with `parent { … }` already selected. `dependencies` is a sibling selection. |
| ServiceNow / CSV | `ServiceNowWorkItemMapper.cs`, `CsvWorkTrackingConnector.cs` | No dependency field exists in either. Out of scope (D13). |
| Feature list UI | `FeatureListDataGrid/columns.tsx` | Column factories: name, forecasts, state, warnings, active work, parent, position, ordering actions. A dependency column is one more factory used by both surfaces. |
| Warnings | `WarningsIndicator.tsx` | Renders exactly two warning kinds today (done-with-remaining-work, default-feature-size), or a green check. Additive by construction. |
| Instance-wide Feature view | `pages/Features/FeaturesView.tsx` | Exists, free, RBAC-filtered result set, built by epic #5375 D17 explicitly to host this epic. |
| Feature DTO | `API/DTO/FeatureDto.cs:76-85` | Already carries additive presentation fields (`Position`, `CanMove`, `MoveBlockReason`, `BlockingPortfolios`) — the pattern this feature's fields follow. |
| Premium gate | `LicenseGuardAttribute` (backend), `hooks/useLicenseRestrictions.ts` (frontend) | Both shipped and in use on `FeaturesController.cs:125`. |
| Terminology | `TerminologyKeys.ts` | `FEATURE`, `FEATURES`, `WORK_ITEMS`, `PORTFOLIOS` — and `BLOCKED: "blocked"`, already owned by epic #5074. See D10. |

---

## Wave: DISCUSS / [REF] Locked Decisions

- **[D1] A dependency is a directed edge between two Features: "this one cannot start until that one is
  done".** Not between work items, not between Portfolios, not between teams. Straight from the epic
  ("Set dependencies on Features"). Work-item-grain dependencies have no consumer — the simulation
  sequences Features, so a work-item edge would ship plumbing, exactly as epic #5375's D1 concluded for
  ordering.

- **[D2] The forecast honours a dependency by EXCLUDING the dependent from the eligible set inside each
  trial, not by shifting its dates afterwards.** In `GetSimulationResultsOfFeatureToUpdate`, a Feature
  whose blockers still have remaining work *in this trial* is not eligible to receive throughput; the
  Features below it move up into the `FeatureWIP` window and consume that capacity instead. This is the
  epic's own word — *jump* — and it is the entire difference between a feature and a cosmetic date
  adjustment. The post-hoc alternative (`max(own, blocker)`) was considered and rejected: it never frees
  the capacity, so every Feature ranked below a waiting one keeps a date that assumes work nobody is
  doing. KPI-2 exists to prove this distinction actually materialises on real data.

- **[D3] One joint simulation across all teams, replacing the per-team independent runs**
  (user, 2026-08-14). `RunMonteCarloSimulation` stops grouping into independent `Task.Run`s; one trial
  advances a shared day clock, and on each day every team with throughput draws and consumes from its
  own rows. **This is distribution-preserving when no dependency is present** — each team consumes only
  its own `SimulationResult` rows, so interleaving them under a shared clock changes the order the RNG
  is drawn in and nothing about the per-Feature completion-day distribution. Existing forecasts
  therefore do not re-baseline beyond Monte Carlo noise, and DISTILL can assert that as a property.
  What it does cost is the parallelism shape: concurrency moves from per-team to per-trial. Flagged to
  DESIGN with the 10,000-trial × all-teams cost as an explicit budget question.

- **[D4] Every dependency comes from the work tracking system. Lighthouse never authors one**
  (user, 2026-08-14, resolving the wave's one open question). The default source is the standard link
  per connector (D14). A Portfolio may **override which field carries them**, exactly as it can for the
  parent: a nullable `DependencyOverrideAdditionalFieldDefinitionId` on `IWorkItemQueryOwner`, beside
  `ParentOverrideAdditionalFieldDefinitionId` (`:27`). The connector's behaviour copies
  `GetParentReferenceForWorkItems` (`AzureDevOpsWorkTrackingConnector.cs:1012-1018`) verbatim in shape:
  when the override is set, **skip the relations fetch entirely** — "no need to load stuff if we have
  an override anyway" — and read the value from `AdditionalFieldValues` instead.
  A per-Feature declaration made inside Lighthouse was considered during this wave and **rejected**:
  it would make Lighthouse an author of dependency data, which is a different product posture from the
  one it takes everywhere else, and it would need a precedence rule against the tracker that this one
  does not.

- **[D15] The override field carries a LIST, and that is where it differs from the parent override.**
  A parent is 0..1, so `GetParentReference` (`:1095-1106`) can return one string. Dependencies are
  0..n, so the field's value is split on comma or semicolon, each entry trimmed, and each resolved
  against the same reference-id space `ParentReferenceId` uses. An entry that resolves to nothing is
  skipped exactly as an unresolvable relation is (AC-1.4) — a text field maintained by hand will
  contain typos, and one typo must not discard the other three references beside it. The separator is
  fixed rather than configurable in this epic; flagged to DESIGN as the one place a per-connection
  setting might later be owed.

- **[D5] Dependency edges are stored in their own persisted relation, never on a synced field.**
  `WorkItemBase.Update` overwrites every synced field on every refresh, which is exactly why
  `Feature.ManualRank` sits outside it. Because D4 makes every edge tracker-sourced, the store needs no
  origin discriminator and has exactly one writer: the sync's reconcile, which replaces a Feature's
  edges wholesale with whatever the current source — link or override field — now says. Expand-only,
  generated with the `CreateMigration` script.

- **[D6] A dependency changes the forecast only when both Features share at least one Portfolio**
  (epic's proposal, user-confirmed 2026-08-14). Feature ↔ Portfolio is many-to-many
  (`LighthouseAppContext.cs:217-219`), so "same Portfolio" means "they share at least one". A dependency
  pointing outside is **detected, stored, shown and warned** — never silently dropped, never forecast.
  The epic's own reasoning holds: a Portfolio's forecast should not depend on Features its viewers may
  not be able to read.

- **[D7] Cycles are detected at ingestion and never reach the simulation.** After every sync, the edge
  set is checked; an edge that closes a cycle is excluded from the honoured set and every Feature in
  the cycle carries a warning naming the loop. Ingestion is the only moment this has to run, because
  it is the only moment edges change (D4). This is the epic's third bullet
  ("must ensure we are not having chains that end up in an infinite loop"), and under D2 it is not a
  nicety: a cycle means no member is ever eligible, so
  `while (simulationResults.GetRemainingItems() > 0)` never terminates. Detection at ingestion keeps the
  simulation loop free of any cycle logic at all.

- **[D8] A dependency whose blocker can never complete in this run is dropped for that run, and warned.**
  Three cases, one rule. A blocker that is **already finished** has no `SimulationResult` row
  (`InitializeSimulationResults` only admits `RemainingWorkItems > 0`) and therefore imposes no
  constraint — that case is free. A blocker whose **team has no throughput** is excluded from
  `RunMonteCarloSimulation` entirely (`Where(g => throughputByTeam.ContainsKey(...))`), so it never
  reaches zero remaining. A blocker **outside the current run's Feature set** likewise never completes.
  In the last two the dependent would wait forever, so the edge is dropped and the dependent is warned
  that its date ignores that dependency. Without this rule the epic ships a hang, and the hang appears
  only on instances that have a team with no recent throughput — which is most of them, eventually.

- **[D9] Premium gates the forecast effect and nothing else.** Verbatim from the epic: *"If we don't
  have a premium license, you will see the warning that the item has a dependency together with the
  hint that it's not taken into account in the forecasting unless you have premium."* This mirrors epic
  #5375's D12 — show the truth, gate the mechanism. Detection, the count, the dialog, the warnings
  **and the Portfolio's dependency-field setting** are all free: the setting is what makes detection
  work at all on an instance whose tracker does not use standard links, and gating it would gate the
  free half of the feature behind a licence.

- **[D10] The UI word is "depends on", never "blocked".** `TERMINOLOGY_KEYS.BLOCKED` and
  `WorkItem.IsBlocked` already name a different, shipped concept from epic #5074, and `blocked` is a
  term instances may rename. Two meanings of one renameable word on the same Feature row is a defect in
  waiting. `Feature`/`Features`/`Portfolios`/`Work Items` resolve through `getTerm` as everywhere else.
  Tracker wording is not UI wording: ADO's "Predecessor" and Jira's "is blocked by" are read, not shown.

- **[D11] Both surfaces are existing ones.** The instance-wide Features view (`FeaturesView.tsx`, built
  by epic #5375 D17 for this) and the existing Portfolio Feature list, both through the shared
  `FeatureListDataGrid` and its `columns.tsx` factory, so the column is written once. The "which items
  do we depend on" list the epic asks for is a dialog opened from the row, following the existing
  work-items dialog pattern. No new page is built by this epic.

- **[D12] Lighthouse never reorders to satisfy a dependency.** A blocker ranked below its dependent is
  the epic's *"dependency ordered lower — that will cause a mess"*, and it is a warning. The forecast
  still honours the dependency and still terminates: the dependent is skipped, capacity flows past it,
  the blocker eventually completes and the dependent becomes eligible. So the situation is not broken,
  it is counter-intuitive — the board disagrees with the stated priority, and the warning is what
  explains that. Ordering stays the user's decision (epic #5375's whole premise).

- **[D13] ADO first, then Jira and Linear. ServiceNow and CSV are out.** ADO is the thinnest first
  connector because `WorkItemExpand.Relations` is already requested on the parent path, and it is the
  bulk of the dogfood instance's data. ServiceNow and CSV have no standard dependency field, so
  supporting them means inventing a convention — a different epic, not a slice of this one.

- **[D14] Only the "waiting on" direction is fetched; the reverse is derived by inverting stored edges.**
  Per ADO #4365's 2026-05-24 comment: ADO `System.LinkTypes.Dependency-Reverse` (Predecessor), Jira
  `issuelinks` where `type.inward = "is blocked by"` **and** an `inwardIssue` is present, Linear's
  `dependencies` connection. Fetching the forward direction too would double the request cost to learn
  something the stored edge set already contains.

---

## Wave: DISCUSS / [REF] Scope Assessment

**Verdict: OVERSIZED as stated, split accepted — with one point for the user to confirm.**

Oversized signals present (2 triggers the gate; 3 are present):

- Touches four areas: work-tracking connectors, the Monte Carlo forecasting engine, the Feature list
  UI, and licensing.
- Contains **two independently valuable user outcomes** that could ship as separate releases: *see the
  dependencies* (slices 01-02) and *forecast against them* (slices 03-04). The first is useful with the
  second never shipping.
- Estimated effort beyond two weeks if taken as one change, principally because of D3.

**Split**: six thin slices, each shipping end to end. The `RunMonteCarloSimulation` restructure (D3) is
**not** a slice — it is a precursor commit inside slice 04, per the slice-composition rule, because it
has no user-visible output of its own.

**Release boundary: deferred, deliberately** (user, 2026-08-14). Slices 01-02 are the visibility half
and 03-04 the forecasting half, which is a natural place to cut a release. The decision is taken after
slice 02 lands and the column is on a real board, rather than now — whether visibility alone is worth
releasing is a question the board answers better than the plan does.

---

## Wave: DISCUSS / [REF] WS Strategy

**Strategy B — extend an existing skeleton.** Brownfield throughout: the Feature view, the shared grid,
the warnings column, the connector port, the licence guard and the ordering switch point are all in
production. No walking skeleton is built. Slice 01 is the thin end-to-end proof — one ADO relation read,
stored, ordered, and rendered as a count on two surfaces.

---

## Wave: DISCUSS / [REF] Driving Ports

| Port | Surface | Introduced by |
|---|---|---|
| Sync (scheduled + manual refresh) | Portfolio Feature fetch gains a relation read; edges are reconciled against stored ones | slice 01 (ADO), slice 06 (Jira, Linear) |
| UI — Features view (`/features`) | A "Depends On" column showing the count of Features this one waits on, plus a dependency warning in the existing warnings column | slices 01, 02 |
| UI — Portfolio detail Feature list | Same column, same factory, no second implementation | slices 01, 02 |
| UI — dependency dialog | Row action opening the list of Features this one waits on: name, state, Portfolio, link to the tracker, and why an edge is not honoured | slice 02 |
| HTTP | `GET /api/latest/features/{id}/dependencies` — free, read-only. No write endpoint exists in this epic (D4) | slice 02 |
| Forecast output | The 50/70/85/95% dates themselves — the port that matters most and the one with no new endpoint | slices 03, 04 |
| UI — Portfolio settings | A **Dependency field** selector beside the existing parent-override selector, listing the connection's additional fields | slice 06 |
| Docs | Dependencies page under the Features documentation, per-feature screenshots | slice 05 |

---

## Wave: DISCUSS / [REF] Pre-requisites

- Epic #5375's `/features` view and shared `FeatureListDataGrid` in `main` — both shipped.
- The dev instance on `:5169` restored from a real backup, so relation reads run against genuine ADO
  data rather than seeded rows. Its measured Feature mix (86 ADO, 4 Jira, 4 Linear) is why ADO is first.
- A premium licence on the verification instance for slices 03-04, and a **deliberately unlicensed**
  profile for KPI-6 and AC-4.7.
- **Real Predecessor links created in the dogfood ADO project** covering: a same-team pair, a
  cross-team pair, a cross-Portfolio pair, a blocker ranked below its dependent, a two-Feature cycle,
  and a blocker whose team has no throughput. Slice 03 cannot be verified without the last three.
  Created directly in ADO with `az boards work-item relation add`, before slice 03 starts — no
  in-Lighthouse authoring exists to create them (D4), which is what moved this from a slice to a
  prerequisite.
- One additional field defined on the dogfood ADO connection, carrying a dependency list, for slice 06.
- `CreateMigration` PowerShell script for the D5 migration, additive only.
- A pre-slice-01 timing baseline of a full ADO portfolio refresh, to defend KPI-3. Epic #5687 just took
  that path from 468,856 ms to 2,087 ms; an N+1 relation read is exactly the shape that gives it back.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Work-item-grain dependencies.** Features only (D1).
- **Writing dependencies back to the tracker.** Lighthouse reads; it never creates a link in ADO, Jira
  or Linear. Same posture as epic #5375's D8 on ordering.
- **Declaring or editing a dependency inside Lighthouse**, in any form — adding one, removing one, or
  suppressing a tracker-sourced one. Considered and rejected under D4: it would make Lighthouse an
  author of dependency data. To change a dependency, change it in the tracker.
- **A configurable *separator* for the override field.** Comma and semicolon, fixed (D15).
- **ServiceNow and CSV.** No standard field exists (D13).
- **Auto-reordering to satisfy a dependency** (D12).
- **Cross-Portfolio forecast effect** (D6) — warned, not simulated.
- **A dependency graph visualisation.** The epic asks for a column, a warning and a list. A graph view
  is a plausible successor and is not designed here.
- **Marketing website copy.** Flagged for the DELIVER checklist under the `Release Notes` tag, not built
  here.

---

## Wave: DISCUSS / [REF] User Stories

Every story traces to a `job_id` in `docs/product/jobs.yaml`. One story is labelled `@infrastructure`
and is a precursor commit inside slice 04, never a slice of its own.

---

### US-01 — A Feature says how many things it is waiting on

`job_id: job-po-see-what-a-feature-is-waiting-on` · persona `product-owner` · **slice 01**

As a product owner, I want every Feature list to show how many other Features it is waiting on, so that
I stop discovering dependencies by accident in a stakeholder review.

### Elevator Pitch
Before: nothing in Lighthouse knows one Feature waits on another; the fact lives only in the tracker.
After: open **Features** in the top navigation → the **Depends On** column reads `2` on a Feature with
two ADO Predecessor links, and `—` on one with none.
Decision enabled: whether this Feature can actually be started now, or whether the team should pull the
next one instead.

**Acceptance criteria**

- **AC-1.1** A Feature with two ADO `System.LinkTypes.Dependency-Reverse` relations to Features in the
  same Portfolio renders `2` in the Depends On column on `/features`.
- **AC-1.2** The same Feature renders the same `2` in the Portfolio detail Feature list — from the same
  column factory, asserted by the column being defined once.
- **AC-1.3** A Feature with no relations renders the empty marker, not `0`.
- **AC-1.4** A relation pointing at a work item that is not a Feature in Lighthouse is skipped, and the
  count excludes it. No error, no partial row.
- **AC-1.5** After a full refresh in which a Predecessor link was removed in ADO, the count drops
  accordingly — stored edges are reconciled, not accumulated.
- **AC-1.6** `Feature.ManualRank` and every synced field are unchanged by dependency ingestion.
- **AC-1.7** The column header resolves through `getTerm(TERMINOLOGY_KEYS.FEATURES)` and contains no
  literal "Epic" or "Blocked".
- **AC-1.8** A full portfolio refresh on the dev instance completes within 110% of the pre-slice
  baseline (KPI-3).

---

### US-02 — See exactly what a Feature is waiting on

`job_id: job-po-see-what-a-feature-is-waiting-on` · persona `product-owner` · **slice 02**

As a product owner, I want to open the list of Features a Feature is waiting on and see their names and
states, so that I can tell whether the wait is nearly over or has not started.

### Elevator Pitch
Before: the count says `2` and nothing says which two, so the answer is still in the tracker.
After: click the **Depends On** cell → a dialog lists each Feature it waits on with name, state,
Portfolio and a link to the tracker record.
Decision enabled: whether to chase the blocker, re-sequence around it, or accept the wait.

**Acceptance criteria**

- **AC-2.1** Clicking the cell opens a dialog listing every Feature this one waits on, each with name,
  state, its Portfolios, and a link that opens the tracker record.
- **AC-2.2** Each row names where it came from — the tracker's own link, or the field this Portfolio
  named (D4). Both are the work tracking system; the distinction is which part of it was read.
- **AC-2.3** An edge that will not be honoured by the forecast is labelled in the dialog with the reason
  in plain language, one of: outside this Portfolio, part of a dependency loop, or the Feature it waits
  on cannot be forecast.
- **AC-2.4** The dialog opens for a user with read access and shows the same content; no action is
  offered without `PortfolioWrite`.
- **AC-2.5** A Feature it waits on that the user cannot read is shown as a redacted row with the reason,
  never omitted silently — a hidden blocker is worse than an unnamed one.

---

### US-03 — Be warned when a dependency will not be honoured

`job_id: job-lead-see-where-the-dependency-chain-breaks` · persona `delivery-lead-rte` · **slice 02**

As a delivery lead, I want the warnings column to flag dependencies Lighthouse cannot act on, so that I
find the broken links by scanning a list rather than by auditing every Feature.

### Elevator Pitch
Before: a cross-Portfolio dependency and a healthy one look identical — both invisible.
After: open **Features** → the warnings column shows a warning on the affected row, whose tooltip reads
e.g. "Waits on a Feature outside this Portfolio — not included in the forecast".
Decision enabled: whether to move a Feature into the Portfolio, re-rank it, or break the loop.

**Acceptance criteria**

- **AC-3.1** A Feature waiting on one in no shared Portfolio renders a warning naming that Feature and
  stating the dependency is not included in the forecast.
- **AC-3.2** A Feature waiting on one ranked **below** it renders a distinct warning naming the
  ordering conflict. No rank is changed by Lighthouse.
- **AC-3.3** A Feature in a dependency loop renders a warning naming the other members of the loop.
- **AC-3.4** A Feature with a healthy dependency renders **no** warning — the presence of a dependency
  is not itself a warning while the forecast honours it.
- **AC-3.5** Existing warnings (done-with-remaining-work, default-feature-size) still render alongside,
  unchanged, and a Feature with none still renders the green check.
- **AC-3.6** No warning text contains the word "blocked" (D10).

---

### US-04 — Read dependencies from the field this Portfolio actually uses

`job_id: job-config-admin-point-at-the-field-that-carries-dependencies` · persona `config-admin` ·
**slice 06** · free

As a configuration admin, I want to tell Lighthouse which field on my Portfolio's work items carries
its dependencies, so that instances that record them in a custom field get everything the standard-link
instances get.

### Elevator Pitch
Before: a Portfolio whose team records dependencies in a custom field reads `—` in the Depends On
column, because Lighthouse only understands the tracker's built-in link type.
After: open **Portfolio → Settings → Advanced**, set **Dependency field** to that additional field, run
a refresh → the Depends On column populates from it, and the relations fetch is skipped entirely.
Decision enabled: whether this Portfolio can use dependency forecasting at all, without anyone
re-recording links in a format the tracker's UI does not offer.

**Acceptance criteria**

- **AC-4.1** With the override set, a Feature whose named field reads `1234;5678` yields two edges,
  and both appear in the count (US-01) and the dialog (US-02).
- **AC-4.2** With the override set, the connector performs **no** relations fetch — asserted on the
  request, mirroring `GetParentReferenceForWorkItems`'s early return (`:1014-1018`).
- **AC-4.3** Comma and semicolon both separate; surrounding whitespace is trimmed; an empty field
  yields no edges and no error.
- **AC-4.4** An entry that resolves to no Feature is skipped and the entries beside it still yield
  edges (D15) — one typo does not discard the list.
- **AC-4.5** With the override **unset**, behaviour is byte-identical to slices 01-05: the standard
  link is read, asserted by an unchanged fixture comparison.
- **AC-4.6** The setting is per Portfolio, offered only for additional fields defined on that
  Portfolio's connection, and requires the same permission the parent override requires.
- **AC-4.7** The setting is available on an unlicensed instance and changes no forecast value there
  (D9) — it feeds detection, which is free.

---

### US-05 — The forecast jumps over a Feature that cannot start

`job_id: job-forecast-honours-what-cannot-start-yet` · persona `delivery-forecaster` · **slice 03** ·
premium

As a delivery forecaster, I want the forecast to give a waiting Feature's capacity to the ones behind it
until its blocker is done, so that the dates reflect the order work can actually happen in.

### Elevator Pitch
Before: the forecast simulates every Feature as startable today, so a Feature that cannot begin for six
weeks gets a date as though it could.
After: open a Portfolio → the waiting Feature's 85% date has moved out, **and at least one Feature
ranked below it has moved in**, because it now gets the capacity.
Decision enabled: whether the delivery date you are about to commit to is achievable at all.

**Acceptance criteria**

- **AC-5.1** Given Features A, B, C in that order, one team, and B waiting on A: B's simulated completion
  is never earlier than A's within a trial.
- **AC-5.2** In the same setup, C's 85% date is **earlier** with the dependency honoured than without —
  the capacity B did not consume went to C. This is the AC that distinguishes D2 from a date shift.
- **AC-5.3** With no dependencies present anywhere, forecast percentiles are unchanged from the previous
  release within Monte Carlo noise, asserted against a fixed random seed.
- **AC-5.4** A dependency whose blocker is in another team is **not** honoured in this slice, and the
  dependent carries a warning saying so (removed by US-08).
- **AC-5.5** A cycle in the data produces the US-03 warning and a forecast that completes in normal
  time — no member of a cycle constrains any other in the simulation.
- **AC-5.6** A blocker whose team has no throughput, or which is absent from the run, is dropped for
  that run and the dependent is warned; the run terminates.
- **AC-5.7** A blocker that is already finished imposes no constraint and produces no warning.
- **AC-5.8** A cross-Portfolio dependency does not change any date (D6).

---

### US-06 — Know the forecast is ignoring a dependency

`job_id: job-forecaster-know-the-forecast-is-ignoring-a-dependency` · persona `delivery-forecaster` ·
**slice 03** · free

As a delivery forecaster on an unlicensed instance, I want to be told that a Feature has a dependency
and that these dates do not account for it, so that I do not present them as though they did.

### Elevator Pitch
Before: an unlicensed instance's dates look exactly as authoritative as a licensed one's, with nothing
to suggest a dependency was ignored.
After: open **Features** without a premium licence → the row shows its Depends On count and a warning
reading "Waits on 1 Feature — dependencies are not included in forecasts without a premium licence".
Decision enabled: whether to trust this date, chase the dependency by hand, or ask for a licence.

**Acceptance criteria**

- **AC-6.1** On an unlicensed instance, every Feature with at least one dependency renders the count,
  the dialog, and the premium hint.
- **AC-6.2** On an unlicensed instance, forecast percentiles are byte-identical to a run with the
  dependency data absent — nothing is silently half-applied.
- **AC-6.3** Licensing the instance and re-running the forecast changes at least one date, with no other
  change of any kind.
- **AC-6.4** The hint names what is withheld and why, and does not use the word "blocked".

---

### US-07 — One joint simulation `@infrastructure`

`job_id: infrastructure-only` · **precursor commit inside slice 04**, never a slice of its own

`infrastructure_rationale`: restructuring `RunMonteCarloSimulation` from one `Task.Run` per team to a
single per-trial loop over a shared day clock produces **no user-visible output on its own** — by
design, since D3's correctness argument is precisely that it changes nothing observable while no
dependency crosses a team. It cannot be released alone and it cannot be verified alone; its verification
is AC-5.3 re-run and US-08's ACs. It lands as the first commit of slice 04, ahead of the story that
needs it.

**Acceptance criteria**

- **AC-7.1** With no cross-team dependency present, every percentile for every Feature matches the
  pre-change run under a fixed random seed, within Monte Carlo noise.
- **AC-7.2** Forecast wall-clock time for the dogfood instance's full Feature set stays within an
  agreed multiple of the pre-change baseline; the number is set by DESIGN and recorded in the slice
  brief before the commit lands.
- **AC-7.3** A team with no throughput is excluded exactly as before, and its Features' behaviour is
  unchanged.

---

### US-08 — Dependencies that cross teams count too

`job_id: job-forecast-covers-dependencies-that-cross-teams` · persona `delivery-forecaster` ·
**slice 04** · premium

As a delivery forecaster, I want a dependency on another team's Feature to move my dates, so that the
most common kind of real dependency stops being the one Lighthouse ignores.

### Elevator Pitch
Before: the warning says "waits on a Feature owned by another team — not included in the forecast", and
that covers most real dependencies.
After: open a Portfolio → that warning is gone and the waiting Feature's 85% date has moved to sit
behind the other team's Feature.
Decision enabled: whether a cross-team commitment is realistic, without building the two forecasts by
hand and comparing them in a spreadsheet.

**Acceptance criteria**

- **AC-8.1** Given team X's Feature B waiting on team Y's Feature A, B's simulated completion is never
  earlier than A's within a trial.
- **AC-8.2** The cross-team warning from AC-5.4 no longer renders for an honoured cross-team edge.
- **AC-8.3** Team X's throughput continues to be drawn from team X's own history — a joint clock shares
  time, never throughput.
- **AC-8.4** A cross-team cycle is detected and warned exactly as a same-team one (D7).
- **AC-8.5** A cross-team blocker on a team with no throughput follows D8 — dropped, warned, run
  terminates.
- **AC-8.6** AC-5.3 still holds: with no dependencies present, dates are unchanged.

---

### US-09 — Jira and Linear dependencies

`job_id: job-po-see-what-a-feature-is-waiting-on` · persona `product-owner` · **slice 05**

As a product owner on Jira or Linear, I want my tracker's dependency links read too, so that everything
slices 01-04 delivered applies to my instance without my re-entering anything.

### Elevator Pitch
Before: a Jira Feature with an "is blocked by" link and a Linear Feature with a `dependencies` entry
both read `—` in the Depends On column.
After: run a refresh → both read their real counts, and their dialogs, warnings and forecast dates
behave exactly as the ADO ones.
Decision enabled: the same decisions as US-01 through US-08, on the tracker the team actually uses.

**Acceptance criteria**

- **AC-9.1** A Jira issue with `issuelinks` containing `type.inward = "is blocked by"` **and** an
  `inwardIssue` yields one edge per such link; entries with only an `outwardIssue` yield none.
- **AC-9.2** A Linear issue's `dependencies` connection yields one edge per node; its `blocking`
  connection yields none.
- **AC-9.3** Adding `issuelinks` to the Jira `fields=` list does not change any existing mapped value —
  asserted by an unchanged fixture comparison.
- **AC-9.4** A ServiceNow or CSV Feature yields no edges and renders no dependency warning — the
  absence of a field is not an error condition.
- **AC-9.5** All of US-02, US-03, US-05 and US-08's ACs pass with Jira- and Linear-sourced edges,
  parameterised over connector rather than duplicated per connector.
- **AC-9.6** A full refresh on Jira and Linear stays within 110% of its own pre-slice baseline.
- **AC-9.7** ADO behaviour is unchanged — the same fixture comparison that guards AC-9.3 for Jira.

---

## Wave: DISCUSS / [REF] Story Map

**Backbone** (user activities, left to right):
*Discover a dependency* → *Understand it* → *Record one Lighthouse cannot read* → *Forecast against it*
→ *Trust it across teams* → *Use it on my tracker*

| Slice | Stories | Outcome shipped | Licence |
|---|---|---|---|
| **01** ADO dependencies visible | US-01 | A Feature list that knows what waits on what | free |
| **02** Detail and warnings | US-02, US-03 | The specifics, and every link Lighthouse cannot act on | free |
| **03** Forecast jumps over a same-team blocker | US-05, US-06 | Dates that account for waiting — and honesty when they do not | premium (US-06 free) |
| **04** Joint simulation, cross-team | US-07 `@infrastructure` (precursor), US-08 | The dependency kind that is actually most common | premium |
| **05** Jira and Linear | US-09 | Everything above, on the other two trackers | as above |
| **06** Per-Portfolio dependency field | US-04 | Instances whose dependencies live in a custom field | free |

**Slice composition gate**: every slice carries at least one user-visible value story. Slice 04's
`@infrastructure` story US-07 is a precursor commit inside it, not a slice.

**Re-ordered 2026-08-14** after the user resolved the wave's open question: dependencies are never
authored in Lighthouse, only read from the tracker, optionally through a Portfolio-named field. The
old slice 03 (declare in Lighthouse) became this slice 06 (name the field), and with it went its
reason to precede the forecast work — it was the only way to seed a cycle onto the dogfood instance,
and it no longer creates edges at all. Those shapes are now created as real Predecessor links in ADO
(see Pre-requisites), and the forecast slices move up two places.

**Carpaccio taste tests**

| Test | Verdict |
|---|---|
| Any slice shipping 4+ new components? | **Pass after re-cut.** The original slice 01 carried ingestion + storage + column + dialog + warnings. Split into 01 (ingest + count) and 02 (dialog + warnings). |
| Every slice depending on a new abstraction? | **Pass.** The dependency-edge store (D5) ships in slice 01 with the smallest thing that uses it, and every later slice reads it rather than extending it. |
| Does any slice disprove a pre-commitment? | **Pass.** Slice 01 can disprove that relation reads are affordable (KPI-3). Slice 03 can disprove that exclusion redistributes capacity at all (KPI-2). Slice 04 can disprove that a joint simulation is affordable (AC-7.2). |
| Synthetic-only data anywhere? | **Pass with one stated exception.** Every slice's dogfood moment runs on `:5169` restored from a real backup, and the awkward shapes (a cycle, a throughput-less blocker) are created as **real ADO links** rather than fixtures. The exception is slice 06: no reachable instance keeps dependencies in a custom field, so its acceptance is fixture-led with one manual confirmation against a deliberately-created additional field. Recorded rather than hidden. |
| Two slices identical except for scale? | **Pass, with one deliberate merge.** Jira and Linear are one slice, not two — same slice shape, and splitting them would produce exactly the pair this test forbids. |

---

## Wave: DISCUSS / [REF] Prioritization

1. **Slice 01** first because it carries the epic's cheapest disprovable claim. Epic #5687 took the
   refresh path from 468,856 ms to 2,087 ms; an N+1 relation read is precisely the shape that gives it
   back, and finding that out costs one slice rather than six.
2. **Slice 02** next because slice 01's count is honest but unactionable, and because the warning
   vocabulary it establishes is what slices 03 and 04 turn off as they deliver. It is also where cycle
   detection lands — one slice before the forecast needs it, which is the cheapest place to get it
   wrong.
3. **Slice 03** is where the epic becomes true, and it is deliberately the *same-team* half: it proves
   the exclusion mechanic, cycle handling and termination against real data without touching
   `RunMonteCarloSimulation`. If D2 is wrong about capacity redistribution, this is the cheapest place
   to find out, and it is now third rather than fourth because nothing needs to precede it.
4. **Slice 04** carries the highest technical risk (D3) and is sequenced after slice 03 has confirmed
   the mechanic it generalises. Reversing them would mean debugging a new simulation loop and a new
   eligibility rule at the same time.
5. **Slice 05** after the mechanic is proven, and the ordering is worth restating because it is not the
   value ordering: ADO is 86 of the dev instance's 94 Features, so it is where the mechanic can be
   dogfooded daily. If the board wants a Jira-first release instead, slice 05 moves ahead of slice 03
   at the cost of two connectors' worth of change before the forecast mechanic is proven.

   The dogfood mix is also why slice 05 is not split per connector: 4 Jira and 4 Linear Features means
   the evidence either slice could produce is thin either way, and splitting doubles the ceremony
   without doubling what is learned.
6. **Slice 06** last because it is the only slice serving a population the dogfood instance does not
   contain. Every instance reachable for verification uses standard links; the Portfolio-named field
   exists for instances that do not, and its acceptance is therefore fixture-led with one manual
   confirmation on `:5169` using a deliberately-created additional field. Shipping it last means it
   inherits a mechanic already proven rather than proving one through an unusual configuration.

**Dogfood cadence**: same-day on `:5169` for every slice. Slices 03 and 04 additionally leave a
before/after date comparison in the slice brief, because "the dates moved" is the only evidence that
matters and it is not visible in a test run.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement | Scope |
|---|---|---|---|
| **KPI-1** Dependencies become visible | 100% of ADO Predecessor links between Features in a shared Portfolio appear in the Depends On column after one refresh; 0 silently dropped | Count stored edges vs. the tracker's own link list on `:5169` | `vendor_demo_only` |
| **KPI-2** The forecast actually moves | ≥ 1 Feature's 85% date moves by ≥ 3 working days with the effect on, **and** ≥ 1 Feature ranked below a waiting one moves **earlier** | Forecast diff with the effect on and off, recorded in the slice 03 brief | `vendor_demo_only` |
| **KPI-3** Sync stays fast | Full portfolio refresh ≤ 110% of the pre-slice-01 baseline, per connector | Update log timing on `:5169`, baseline captured before slice 01 | `per_instance` + `vendor_demo_only` |
| **KPI-4** The forecast always terminates | 0 runs exceeding the pre-epic p99 duration across 5 consecutive scheduled refreshes, with a cycle, a throughput-less blocker and a cross-Portfolio edge all present in the data | Scheduled runs on `:5169` with the seeded shapes from the prerequisites | `per_instance` |
| **KPI-5** One eligibility decision | Exactly 1 place in the codebase decides whether a dependency is honoured | Grep at DISTILL; ArchUnit-style assertion if one fits | `per_instance` |
| **KPI-6** Free-tier honesty | On an unlicensed instance: 100% of Features with a dependency show the hint, and 0 forecast values differ from a dependency-free run | E2E against the unlicensed profile (AC-6.1, AC-6.2) | `per_instance` |
| **KPI-7** Cross-team coverage | ≥ 80% of the dogfood instance's detected same-Portfolio dependencies are honoured after slice 04, versus the same-team subset after slice 03 | Count honoured vs. detected edges before and after | `vendor_demo_only` |

KPI-2 is the one that decides whether this epic was worth building. If no Feature below a waiting one
moves earlier, D2 reduces to a date shift and the design is wrong, not the data.

---

## Wave: DISCUSS / [REF] Definition of Done

1. All acceptance criteria for the slice pass as automated tests.
2. `dotnet build` zero warnings; `dotnet test` green.
3. `pnpm test`, `pnpm build` (zero warnings), Biome clean — stated explicitly as N/A per slice where
   there is no frontend change.
4. Mutation testing ≥ 80% kill rate on the changed backend surface. Non-negotiable on the eligibility
   rule and the cycle detector, where a surviving mutant is a hang or a wrong date rather than a metric.
5. SonarQube Cloud: no new issues of any severity, including security hotspots.
6. EF migration generated with the `CreateMigration` script, additive only.
7. Docs updated per-feature, in the seeded terminology, with per-feature screenshots.
8. ADO story transitioned; slice pushed only after CI is green.
9. The slice's learning hypothesis has an explicit verdict recorded in its brief — confirmed or
   disproved, never blank.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Verdict | Evidence |
|---|---|---|---|
| 1 | Business value articulated | ✅ | The forecast is the product's core output and is computed as though nothing waits on anything. KPI-2 and KPI-7 carry the outcome |
| 2 | Job traceability | ✅ | 6 jobs written to `docs/product/jobs.yaml`; all 8 value stories carry a real `job_id`; US-07 is `@infrastructure` with a rationale and is a precursor commit inside slice 04 |
| 3 | Acceptance criteria testable | ✅ | 55 ACs, each observable from a rendered cell, a dialog, a tooltip, an HTTP status, a stored edge, a forecast percentile, an outbound request, or a wall-clock measurement |
| 4 | Dependencies identified | ✅ | Epic #5375's Feature view shipped; `:5169` restored from a real backup; real Predecessor links created in ADO; premium + unlicensed profiles; `CreateMigration`; a pre-slice-01 timing baseline |
| 5 | Sliced ≤ 1 day each | ⚠️ | 6 briefs. Five are 4-6h. **Slice 04 is the exception** — D3's restructure is bounded by AC-7.2, whose number DESIGN has not set. The brief carries a timeboxed probe and is re-cut if the probe says the parallelism change is larger than a day |
| 6 | No known blockers | ✅ | None. The wave's one open question (where a dependency comes from) was resolved by the user on 2026-08-14; D4, D5, D9, D15, US-04 and the slice order were rewritten to match |
| 7 | Observable surface defined | ✅ | Driving Ports table; the forecast dates themselves are named as the port that matters and the one with no endpoint |
| 8 | Test data / environment available | ⚠️ | `:5169` has real ADO/Jira/Linear Features but contains no cycle, no throughput-less blocker and no dependency-carrying custom field. All three are created directly in ADO before slice 03, because D4 leaves Lighthouse no way to author them |
| 9 | Outcome KPI with numeric target | ✅ | 7 KPIs, each with a number or a binary and a named measurement source |

**Requirements completeness: 0.96.** The missing 0.04 is items 5 and 8, both of which are stated as
open with a plan rather than guessed at.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key decisions

See Locked Decisions above (D1-D14). The five that shape everything downstream:

- **[D2]** Exclusion inside the trial, not a date shift afterwards. It is the difference between this
  feature and a cosmetic one, and KPI-2 is written to falsify it.
- **[D3]** One joint simulation across teams — the epic's promise is unreachable without it, and it is
  distribution-preserving where no dependency crosses a team, which is what makes it safe to ship.
- **[D7] + [D8]** Cycles excluded at ingestion and unforecastable blockers dropped per run. Under D2
  both are termination guarantees, not polish: without either, the simulation's `while` loop does not
  end.
- **[D9]** Detection free, forecast effect premium — the epic's own wording, and epic #5375's D12 shape.
- **[D4] + [D15]** Lighthouse reads dependencies and never authors them; a Portfolio may name the field
  they live in, copying the parent override. The list-valued field is the one place the two mechanisms
  genuinely differ.
- **[D11] + [D10]** Two existing surfaces and one deliberate refusal of the tracker's vocabulary,
  because `blocked` already names a different shipped concept and is renameable.

### Requirements summary

- **Primary needs**: a forecast that knows what cannot start yet and gives that capacity to what can; a
  Feature list that says what each Feature waits on; a way to read dependencies from whichever field a
  Portfolio actually keeps them in; and an unlicensed instance that says plainly it is ignoring one.
- **Walking skeleton scope**: none built (strategy B). Slice 01 is the thin end-to-end proof through the
  existing sync, storage, ordering and grid path.
- **Feature type**: cross-cutting.

### Constraints established

- The Monte Carlo simulation must terminate on every input, including cycles, self-references and
  blockers that can never complete. This constrains the design more than any functional requirement.
- Existing forecasts must not re-baseline. D3 is only acceptable because it is distribution-preserving
  in the absence of dependencies, and AC-5.3 / AC-7.1 assert that under a fixed seed.
- Sync speed is a protected asset after epic #5687. Relation reads are additive to a path that is now
  225× faster than it was, and KPI-3 defends it.
- Lighthouse never writes to the tracker — no link is created, changed or removed there — and, per D4,
  never authors a dependency on its own side either. It reads, in both directions of that sentence.
- Expand-only migrations, generated with `CreateMigration`.
- Terminology is the instance's own; `blocked` is off-limits for this concept.
- **No commit lands without the maintainer's explicit approval** (user, 2026-08-14). This epic edits the
  Monte Carlo loop every date in the product comes from, so the usual "green then commit" autonomy is
  suspended for its whole length: DISTILL and DELIVER stop and ask before every commit, including
  test-only and refactor commits.

### Upstream changes

None. No DISCOVER or DIVERGE wave ran for this feature, so no prior assumption was altered. Epic
#5375's D17 anticipated this feature and is carried forward intact rather than revised.

---

## Wave: DISCUSS / [REF] SSOT Updates

- `docs/product/jobs.yaml` — 6 jobs appended, `epic-4365-dependencies` added to `feature_context`.
- `docs/product/journeys/epic-4365-dependencies.yaml` — created; 3 journeys.
- `docs/product/personas/product-owner.yaml` — 1 job appended to `primary_jobs`.
- `docs/product/personas/delivery-forecaster.yaml` — 3 jobs appended.
- `docs/product/personas/delivery-lead-rte.yaml` — 1 job appended.
- `docs/product/personas/config-admin.yaml` — 1 job appended.

---

## Wave: DISCUSS / [REF] Peer Review

Not invoked. The mandatory consolidated review fires at the end of DISTILL with all four waves visible.
Per-wave review triggers were checked: DoR carries two ⚠️ items, but both are stated-open-with-a-plan
rather than ambiguities a reviewer could resolve, and neither blocks DESIGN. No vendor-neutrality risk —
the connector-specific reading is confined to D14 and US-09, and D13 states plainly which trackers are
out and why.

---

## Wave: DISCUSS / [REF] Handoff

**To**: `nw-solution-architect` (DESIGN) — full artifact set. `nw-platform-architect` (DEVOPS) — the
Outcome KPIs section only.

**Open questions carried into DESIGN**

- **OQ-1 — RESOLVED** (user, 2026-08-14). The epic meant the per-Portfolio pointer at a tracker field,
  in the shape of `ParentOverrideAdditionalFieldDefinitionId`, once per Portfolio. D4, D5, D9, US-04
  and the slice order were rewritten; D15 was added for the list-parsing difference. Kept here rather
  than deleted because DESIGN should know the per-Feature declaration was considered and dropped, not
  overlooked.
- **OQ-2** — The trial-cost budget for D3. Ten thousand trials over all teams jointly, versus per-team
  in parallel: what multiple of today's wall clock is acceptable, and does concurrency move to
  per-trial or to trial batches? AC-7.2's number comes from this answer, and slice 04's estimate depends
  on it.
- **OQ-3** — Whether the override field's value is resolved against `ReferenceId` alone, or whether a
  tracker-specific form (an ADO id, a Jira key, a URL) needs normalising first. D15 fixes the
  separator; it does not fix what an entry between separators looks like, and a hand-maintained field
  will contain all three.
- **OQ-4** — Where the single eligibility decision lives so that KPI-5 is structurally true rather than
  defended by a grep: inside `SimulationResult`, in a small collaborator consulted by
  `GetSimulationResultsOfFeatureToUpdate`, or in the set handed to `InitializeSimulationResults`.
- **OQ-5** — Whether cycle detection runs over the whole edge set on every sync, or incrementally per
  changed Feature. At the dogfood instance's size either is free; at ten thousand Features it is not.
