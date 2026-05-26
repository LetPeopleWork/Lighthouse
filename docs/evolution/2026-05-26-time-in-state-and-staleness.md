<!-- DES-ENFORCEMENT : exempt -->

# Evolution Archive — time-in-state-and-staleness (Finalize)

**Feature ID**: `time-in-state-and-staleness`
**Epic**: ADO #4144 — More Detailed State Info (https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/4144). Covers Epic slices **A + B1 + D**.
**Waves shipped**: DISCUSS -> DESIGN -> DISTILL -> DELIVER (delivered across three slices + a #5088 fast-follow, ~2026-05-24 -> 2026-05-26).
**HEAD at finalize**: `4d5ba41e` (`test(time-in-state): mutation-harden staleness surface to >=80% kill`)
**Status**: Feature complete and shipped to `main`. All 7 E2E walking-skeletons live-verified on demo data (zero `test.fixme` remaining). CI green incl. `verifypostgres`; SonarCloud gate green; mutation gate passed both stacks. ADO stories closed.
**Long-form artifact**: `docs/feature/time-in-state-and-staleness/feature-delta.md` (the authoritative DISCUSS+DESIGN+DISTILL+DELIVER record; this file is the close-out summary). The workspace is preserved (the wave matrix derives status from it).

---

## Feature summary

Per-item **time-in-state** measurement plus an opt-in **staleness** Flow Signal. Every work item carries a new `CurrentStateEnteredAt`, rendered as a `<n>d in <state>` badge in the WIP dialog; when a team/portfolio opts into a staleness threshold, items past it surface red across three surfaces — the dialog badge, a new **Stale Items** overview widget (count + RAG + view-data), and the **Work Item Aging Chart** (red stale bubbles + Time-in-State on bubble click + `isStale` in the chart RAG). A single client-side `deriveStaleness(item, thresholdDays, now)` selector is the sole home of the predicate and the blocked-excludes-stale rule, so the surfaces can never disagree. The data foundation is a new `WorkItemStateTransition` entity captured at sync time from each connector (Jira/ADO via existing changelog/revision parsing, Linear via a new GraphQL `history` connection with per-connection runtime downgrade, CSV via an optional "Current State Since" column).

## Business context

Flow coaches running a team's flow review need to spot stuck items at a glance, before the cycle-time distribution degrades — without opening each item in Jira/ADO to read its history tab. The Epic is community/Productboard-validated (no formal DISCOVER artifact). The `WorkItemStateTransition` foundation is the strategic investment: it unlocks the rest of Epic 4144 (B2, B3, C, and F=`aging-pace-percentiles` all reuse it; only E needs its own mechanism).

## Key decisions

### Architectural (ADR-backed)

- **ADR-015** — `WorkItemStateTransition` is a standalone entity with FK→WorkItem (not an owned collection, not a JSON column) — read-path performance for the work-item table (DDD-1).
- **ADR-016** — `WorkItem.CurrentStateEnteredAt` is a sync-time-persisted column, not a query-time MAX over transitions — one SELECT, zero subqueries to render the badge (DDD-2). `WorkItemService.RefreshWorkItems` is the sole writer/mutator; ArchUnit-enforced (DDD-6).
- **ADR-017** — Transition-capture dispatch is a per-connector capability with sync-delta fallback in `WorkItemService`; transition idempotency by `(WorkItemId, ToState, TransitionedAt)`; Linear per-connection runtime downgrade (DDD-3/7/10).
- **ADR-018** — Shared `IPerStateAggregationService` is NOT introduced; sibling consumer DESIGNs (`aging-pace-percentiles`, `state-time-cumulative-view`) consume `IWorkItemStateTransitionRepository` directly because their windowing semantics diverge (DDD-4).
- **ADR-026** — A single client-side pure `deriveStaleness` selector is the sole home of the staleness predicate + blocked-exclusion; the three surfaces consume it; DDD-8 (client-side, no sync round-trip) upheld (DDD-13).

### Domain / design invariants

- **DDD-5** — `StalenessThresholdDays` lives on `WorkTrackingSystemOptionsOwner` (inherited by Team and Portfolio); avoids two parallel migrations.
- **DDD-8** — Per-render staleness comparison is client-side, driven by `currentStateEnteredAt` + `stalenessThresholdDays`; threshold edits take effect on next render, no sync.
- **DDD-9** — `WorkItemDto.Approximate: bool` carries the badge-tooltip capability distinction; sibling consumers never branch on transition origin.
- **DDD-13** — Blocked-excludes-stale applied uniformly on every surface (a blocked-over-threshold item counts as Blocked, not Stale), including retroactively on the existing WIP-dialog badge.

### Late decisions (slice 03 — staleness reframed as a first-class opt-in Flow Signal)

- **DDD-12 supersedes DISCUSS D8** — Staleness is **opt-in**: default `0` (off) for new teams + portfolios (was 7/14), relocated into the generic `FlowMetricsConfigurationComponent` (checkbox → seed 5 team / 14 portfolio → 0 on disable); the standalone "Flow Signals" settings group was removed. Existing slice-02 owners keep 7/14 (no reset).
- **DDD-3 refinement** — `IWorkTrackingConnector.SupportsTransitionHistory` became a connection-parameterized method so CSV answers per-connection (true only when the state-since column is configured).
- **DDD-14/15** — Stale Items widget reuses Blocked's `count >= 2` "high" RAG; the aging chart factors `isStale` symmetrically with `isBlocked`.

## Stories shipped

| Story | ADO | Outcome |
|---|---|---|
| US-01 — time-in-state badge (team) | #5026 (slice 01) | `<n>d in <state>` column in the WIP dialog; `—` until a baseline exists; `+1`-day convention matched to Work Item Age. |
| US-02/03/04 — staleness threshold + settings (team + portfolio) | #5027/#5028 (slice 02) | `StalenessThresholdDays` persisted + validated `[0,365]` on Team & Portfolio; RBAC-gated settings field. |
| US-05 — staleness as an opt-in Flow Metric | #5089 (slice 03) | Relocated into `FlowMetricsConfigurationComponent`; default off; old group removed. |
| US-06 — Stale Items overview widget | #5090 (slice 03) | Count + RAG + view-data, cloned from Blocked Items; count excludes blocked. |
| US-07 — staleness in the Work Item Aging Chart | #5091 (slice 03) | Red stale bubbles + Time-in-State on click + `isStale` in chart RAG, via `deriveStaleness`. |
| Portfolio Feature transitions | #5088 (fast-follow) | Linear `Project.history` added to the projects query + mapped to states; Jira/ADO already captured via the shared converter. |

## Steps completed

DELIVER ran across three roadmaps. Slice 01 (US-01) and slice 02 (US-02/03/04) shipped first; slice 03 (`roadmap.json`, 11 steps US-05/06/07) executed 01-01 .. 07-01; the #5088 fast-follow followed. Demo data became demoable via an optional CSV "Current State Since" column (`1200f791`): connector emits `SyncedTransitions` → `RefreshWorkItems` derives `CurrentStateEnteredAt` (DDD-6 intact).

**Execution-log reconciliation**: `deliver/.develop-progress.json` records step `07-01` (the slice-03 walking-skeleton E2E) as still pending / `all_steps_done: false`. This is a **stale-marker false-negative** — `07-01` was held `CHECKPOINT_PENDING` (E2E un-verified) when the log was last written, then completed via direct commits outside the formal nw-execute loop (`daa5f4a0` un-fixme portfolio column, `2d760c15` un-fixme widget, `eb2aae44` stale bubbles, `04464fed`/`0e965c79`/`a47f8096` verifypostgres de-flake). Verified at finalize: `TimeInStateAndStaleness.spec.ts` has **7 active scenarios, zero `.fixme`/`.skip`**, and `verifypostgres` is green. The feature is genuinely complete; the marker simply was not written back.

Representative slice-03 + #5088 commits: `b367cbc7` (US-05), `831245b6`+`eb2aae44` (US-07 selector + chart), `e0125152`+`2d760c15` (US-06 widget), `1200f791` (CSV state-since), `aa325466`+`daa5f4a0` (#5088 Linear `Project.history`), `e3403e26`+`eb2aae44` (live-E2E-surfaced fixes).

## Quality gates

- **CI green** on `main` incl. the previously-flaky `verifypostgres` (run for `a47f8096`); SonarCloud gate green; DDD-6 ArchUnit (RefreshWorkItems sole writer) holds.
- **Mutation testing — PASS both stacks** (feature-scoped to the diff): backend Stryker.NET **87.1%** (88/101), frontend Stryker **85.0%** (102/120). 11 survivors killed via new tests; remainder justified (log-only, equivalent guards, MUI styling). Reports in `deliver/mutation/`. Committed `4d5ba41e`.

## Lessons learned

- **`[NotMapped]` init-only sync transients are dropped by the `WorkItem(base, team)` copy-ctor** (`Update()` can't set `init`). Connector states are RAW; `WorkItemBase.State` is MAPPED; new items have `Id=0` until Save. All three bit time-in-state and were caught by live E2E, not mocks. (memory `project_workitem_sync_transient_and_state_mapping_gotchas`)
- **React #185 infinite re-render** from an inline `new Date()` default feeding a `useEffect` dep in the aging chart — blanked the metrics view. Unit tests missed it; live Playwright caught it. Fix: `useMemo`-stabilize the `now` default. (`eb2aae44`; memory `project_react185_loop_unstable_useeffect_dep`)
- **`verifypostgres` flake whack-a-mole**: client-side staleness scenarios (DDD-8, provider-invariant) ran on the slow Postgres CI path and flaked through a sequence of races (duplicate ReferenceIds → re-sync throw; widget count read before async load; `enableStaleness` checkbox reset by a late settings-load `useEffect`; red-badge/aging-chart count snapshots). Each fixed with `expect.poll` / demo-data dedupe / load-reset hardening. Durable note: these scenarios add **zero** Postgres-specific coverage — a candidate to scope out of `verifypostgres` rather than poll-patch further.
- **Stryker.NET 4.14.2 `{a..b}` line-range glob silently mutes ALL mutants** in this environment — a `{a..b}`-scoped run can look like a vacuous PASS. Mutate whole-file + filter survivors to feature lines during analysis. FE Stryker line-ranges are unaffected. (captured in `docs/ci-learnings.md`)
- **Demo data needed transition history** to be demoable (CSVs had none → `currentStateEnteredAt` null → "—"); the optional CSV "Current State Since" column closes that for demo + sibling time-in-state E2E. (memory `project_demo_data_time_in_state_via_csv_column`)

## Issues encountered

- `verifypostgres` flakiness (above) — resolved, not a product defect; SQLite + throwaway-Postgres both passed all 7 walking-skeletons throughout.
- Stryker.NET line-range tooling defect (above) — worked around, documented.

## Migrated permanent artifacts

No standard-destination-map artifacts existed to migrate: this feature used the lean `feature-delta.md` + `slices/` workspace structure (no `design/architecture-design.md`, no `design/adrs/ADR-*.md` files, no `distill/walking-skeleton.md`, no `discuss/journey-*`). The ADRs referenced above (ADR-015..018, ADR-026) are catalogued inside `feature-delta.md`'s decision tables rather than as standalone files.

### Preserved in workspace (lasting value, not deleted)

- `docs/feature/time-in-state-and-staleness/feature-delta.md` — authoritative DISCUSS+DESIGN+DISTILL+DELIVER record.
- `docs/feature/time-in-state-and-staleness/slices/slice-01-time-in-state-mvp.md`, `slice-02-staleness-threshold.md`.
- `docs/feature/time-in-state-and-staleness/deliver/mutation/` — three mutation reports (committed `4d5ba41e`).
- `docs/feature/time-in-state-and-staleness/deliver/roadmap.json`, `roadmap-slice-01.json` — committed at finalize.

### Discarded (gitignored session scaffolding)

- `deliver/execution-log.json` (DES audit trail — captured in this evolution doc) and `deliver/.develop-progress.json` (resume marker) are gitignored; never committed.

## References

- Long-form: `docs/feature/time-in-state-and-staleness/feature-delta.md`
- Mutation: `docs/feature/time-in-state-and-staleness/deliver/mutation/mutation-report.md`
- CI learnings: `docs/ci-learnings.md` (Stryker.NET line-range entry, 2026-05-26)
- Next in Epic 4144: `docs/feature/aging-pace-percentiles/` (feature F — DESIGN complete, DISTILL next)
