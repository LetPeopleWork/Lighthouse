# Portfolio Blocked History — Evolution

**Feature:** portfolio-blocked-history | **ADO:** #5524 | **Epic:** follow-on to Epic 5074 (Blocked Items) | **Shipped:** 2026-07-22

## What shipped

Brought Portfolio blocked metrics to full parity with Teams. Before this feature, a Portfolio's historic Blocked count and its Blocked-Over-Time drill-through were dishonest: history was not recorded for features, so any past range re-evaluated *today's* blocked rules, and clicking a past bar returned nothing. Five slices closed the gap through a dedicated portfolio-scoped `FeatureBlockedTransition` keyspace (ADR-102/103), keeping the team keyspace uncorrupted (the original defect that motivated slice 01).

| Slice | Step(s) | Outcome |
|---|---|---|
| 01 — stop demo backfill corrupting team history | 01-01, 01-02(doc) | Demo backfill no longer writes Feature.Ids into `WorkItemBlockedTransition`; FK-InMemory ci-learning recorded. |
| 02 — capture feature blocked spells | 02-01…02-04 | New `FeatureBlockedTransition` entity + portfolio-scoped repo; `FeatureBlocked`/`FeatureUnblocked` events + capture/close handlers; `FeatureDto` blocked routed through `IBlockedItemService`; spells captured in the `RefreshFeatures` seam. |
| 03 — honest historic blocked count | 03-01, 03-02(doc) | Portfolio wip read answers past ranges from captured per-portfolio spells; obsolete "not recorded for features" caveat removed from `docs/metrics/widgets.md`. |
| 04 — drill into a past portfolio blocked bar | 04-01 | `GetBlockedItemsAtDate` reconstructs past membership READ-ONLY from feature spells (never persisted); ADR-104 departed-feature intersection; today/live branch preserved. |
| 05 — demo portfolio blocked history | 05-01 | Demo refresh synthesizes backdated feature spells in the feature keyspace, drillable with real durations; idempotency reconciled off feature-spell presence. |

## Key decisions & design anchors

- **Dedicated portfolio keyspace (ADR-102/103):** `FeatureBlockedTransition` keyed `(FeatureId, PortfolioId)`, separate from `WorkItemBlockedTransition`. Resolves the Feature.Id ↔ WorkItem.Id collision that made the old cross-keyspace read impossible.
- **Reconstruction, never persistence (`blockedMembershipAtDate` SSOT):** past membership is rebuilt on read from half-open spell intervals (`EnteredAt < startOfNextDate && (LeftAt == null || LeftAt >= startOfDate)`); no membership column was added to `BlockedCountSnapshot`.
- **ADR-099 reconciliation guard:** the drill-through count is cross-checked against the independently-captured `BlockedCountSnapshot`; a divergence logs a capture-gap warning. It **stayed quiet** across the test window (reconstructed == snapshot) — an independent confirmation that slice-02 capture is sound.
- **Mirror of the team shape:** `PortfolioMetricsController.GetBlockedItemsAtDate` mirrors `TeamMetricsController.cs:499-526`; the two drill-throughs now share structure.

## Gotchas worth remembering

- **Demo idempotency trap (slice 05):** the legacy idempotency guard keyed off *snapshot* presence. After slice 01, demo portfolios carry backdated snapshots but no feature spells, so a snapshot-gated write short-circuits and synthesizes nothing on exactly the targeted instances. Fix: feature-spell synthesis runs **upstream** of the snapshot guard, idempotent per `(portfolio, feature)` via `GetOpenSpell`. Only a feature-spell-based guard satisfies both "second refresh adds nothing" and "snapshots-exist-but-no-spells still synthesizes."
- **Repository set-scan discipline:** `GetByPredicate`/`Exists` use `SingleOrDefault` and throw on multiple matches — use `GetAllByPredicate(...).Any()` / `GetOpenSpellsForPortfolio` for window scans.
- **Today vs past boundary:** both `targetDate` and `today` are `DateOnly.FromDateTime(...Date)`; the `targetDate >= today` split is a pure date comparison — no UTC time-of-day leakage.

## Quality gates

- **Acceptance:** slice-04 5/5, slice-05 7/7 green; full backend suite green (3549 tests; `JiraWriteBackTest` live-Jira flaky excluded — ADO Bug 5542).
- **Build:** `dotnet build` zero-warning (`TreatWarningsAsErrors`).
- **EF migrations:** `AddFeatureBlockedTransition` generated for both Sqlite and Postgres providers, expand-only (new table).
- **Refactor (L1-L6):** no changes warranted — code matches sanctioned team shapes.
- **Adversarial review:** no confirmed defects; acceptance tests verified honest (row/response-level assertions, non-vacuous preconditions, demo-gate + team-keyspace invariant + StartedDate cap all covered).
- **Mutation (per-feature, Stryker.NET):** **100%** on the feature's dedicated new components — capture handler, close handler, and `FeatureBlockedTransitionRepository` (33/33 mutants killed). Three added boundary tests kill the half-open interval + portfolio-scoping mutants (`GetBlockedTransitionsAt`, `GetFeatureIdsWithBlockedHistory`). The changed logic in the shared `PortfolioMetricsController` (drill-through reconstruction) and `DemoBlockedHistoryBackfillHandler` (portfolio demo synthesis) carried **zero surviving mutants** in the full-file run. A sub-80% aggregate over whole *touched* files reflects pre-existing untested construction code (`FeatureDto`, `UpsertSnapshot`, other controller endpoints), not this feature's changed surface.
- **Frontend:** no production change shipped — `pnpm`/Biome gates N/A (verified: zero FE files in the feature diff).
- **DES integrity:** all 10 steps carry complete traces.

## Deferred to user decision at finalize

- **Screenshots (DoD #8):** the portfolio Blocked-Over-Time drill-through and demo chart now populate where they were empty; a live `@screenshot` regen against a clean backend would refresh `docs/assets/`. Not auto-run.
- **ADO #5524** state transition + `Release Notes` tag decision.
- **Push:** the whole feature stack is unpushed at time of writing.

## Retrospective note

Clean execution. The one process wrinkle was environmental, not the feature: subagent tooling (native Read/Grep/Glob and `ctx_*`) was unavailable in this session, so the adversarial-review dispatch ran blind and was re-done inline on the main thread. `roadmap.json` also went missing mid-feature and was regenerated. Neither affected the shipped code.
