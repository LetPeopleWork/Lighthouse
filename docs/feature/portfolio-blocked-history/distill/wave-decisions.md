# DISTILL Wave Decisions — portfolio-blocked-history

**Feature**: portfolio-blocked-history (ADO #5524)
**Date**: 2026-07-20
**Prior-wave decisions reconciled**: PASSED — 0 contradictions across DISCUSS D1-D7, DESIGN DDD-1..DDD-7, ADR-102/103/104 (Accepted 2026-07-20)
**Reconciliation gate**: 0 contradictions logged

---

## DST-1: Test infrastructure — SQLite mandatory, InMemory forbidden for FK-dependent assertions

**Decision**: Every test scaffolded for this feature inherits from `PortfolioBlockedHistoryAcceptanceTest`, which uses `WebApplicationFactory<Program>` backed by **real SQLite** (`EnsureDeleted()` + `EnsureCreated()` per `[SetUp]`). EF InMemory is NOT used — the original `DemoBlockedHistoryBackfillHandler` defect shipped because InMemory does not enforce foreign keys (ci-learnings candidate OQ-5).

**Rationale**: ADR-102 "Earned Trust" section explicitly requires SQLite for FK-dependent probes. The `PortfolioBlockedHistoryAcceptanceTest` base class was created for exactly this reason during epic-5074 slice-08. Every slice-01 through slice-05 scenario that touches `FeatureBlockedTransition`, `WorkItemBlockedTransition`, or `BlockedCountSnapshot` runs against a real SQLite database.

**OQ-5 ci-learnings entry**: Filed in `distill/upstream-issues.md` — "EF InMemory does not enforce FKs; any test asserting FK-dependent behaviour must run on SQLite." Awaiting PR merge into `docs/ci-learnings.md` by DELIVER or the `/clean-ci` maintainer.

---

## DST-2: No .feature files — C# scenario/specification split convention

**Decision**: Acceptance tests use the established Lighthouse pattern: `*Scenarios.cs` (test methods with business-language names, XML doc-comment `@` tags) + `*Specifications.cs` (Given/When/Then step-method partial class). No Gherkin `.feature` files — the repo has never used them, and the pattern is idiomatic NUnit.

**Rationale**: Project conventions ALWAYS win (Mandate 1.0). The `BlockedItems/` directory already follows this pattern for epic-5074 slices 01-08. New scenarios for portfolio-blocked-history extend the pattern. The XML doc-comment `@walking_skeleton`, `@driving_port`, `@us-NN`, `@contract-shape:*` annotations are the C#-convention equivalent of Gherkin tags.

**Authoring precedent**: `Slice02FeatureBlockedCaptureScenarios.cs` + `Slice02FeatureBlockedCaptureSpecifications.cs` (DISTILL, 2026-07-20) — the first portfolio-blocked-history pair.

---

## DST-3: No PBT framework — FsCheck not in repo

**Decision**: No property-based testing framework is added. The repo does not use FsCheck. The `@property` and `@invariant` annotations on scenarios signal that specific invariants are verified, but they are implemented as traditional example-based or parametrized NUnit tests — not generative PBT.

**Rationale**: The polyglot matrix reserves PBT (Mandate 9) for layers 1-2 (unit, in-memory acceptance). These are layer 4 integration tests (real HTTP, real SQLite) — PBT at this layer is incompatible with test cost (~100ms per example vs. PBT's 100+ examples). The README states "No PBT framework in repo per CLAUDE.md" — adding FsCheck would be a separate infrastructure decision outside DISTILL scope.

**Mitigation**: Each slice includes at least one `@property` or `@invariant` scenario covering a universal invariant (e.g., the parity-matrix test in slice 03, the keyspace-purity guard in slices 01 and 05). These are example-pinned per Mandate 11 (layer 3+ sad paths are example-based, never PBT-generated).

---

## DST-4: No E2E — Playwright suite unchanged

**Decision**: No Playwright E2E tests are added. The feature changes backend read/write paths only; the frontend is fully shared (C4 / ADR-103 §3) and already covered by the existing epic-5074 Vitest + Playwright suites. Adding a Playwright test for portfolio blocked drill-through would duplicate the integration test's assertion through a slower, flakier path.

**Rationale**: The DESIGN explicitly states "Frontend: NONE" — and C4 proves `PortfolioMetricsView.tsx:64` renders the same `BaseMetricsView` as `TeamMetricsView.tsx:132`. The existing `BlockedItems.spec.ts` E2E covers the Blocked overview widget's shared components; portfolio scope is already exercised by `PortfolioDetail` and `DeliveryMetrics` E2Es. Adding another is Testing Theater — it would assert the backend's response shape through the browser, which the integration test already asserts through the HTTP client.

**Explicit record**: E2E is N/A — not skipped silently (per standing rule).

---

## DST-5: Frontend — N/A (confirmed zero work, not skipped silently)

**Decision**: Zero frontend test changes. US-02 AC6 forbids portfolio-specific widgets or branches. C4 proved the frontend is fully shared and inert only because `FeatureDto` passes `null` for `blockedSince` (`FeatureDto.cs:18`). Populating that field (slice 02) lights up all three blocked surfaces through existing shared components.

**Verification**: `grep` for portfolio-conditional logic in `Lighthouse.Frontend/src/pages/Common/MetricsView/` confirms zero portfolio-specific branches exist. The existing Vitest suite for `blockedMaxAgeRag.ts`, `deriveStaleness.ts`, and `BlockedOverviewWidget` already covers the shared logic.

---

## DST-6: Outcomes registry — skipped (absent, not methodology-only)

**Decision**: `docs/product/outcomes/registry.yaml` does not exist in this repo. Outcome registration is skipped. This is NOT a methodology-only feature — it introduces new typed contract surfaces (two new domain events, one new entity, two new read paths) — but the registry tooling and SSOT are absent. Recorded as explicit skip per the graceful-degradation matrix (warn, proceed).

**Mitigation**: The DISCUSS parity matrix (9 rows → all `none` at feature close) serves as the outcome-tracking mechanism. The two new domain events (`FeatureBlocked`, `FeatureUnblocked`) and the two new read paths (`inProgressFeatures?asOfDate`, `blockedItemsAtDate?date`) are documented in the driving-ports table in `feature-delta.md`.

---

## DST-7: Tier A only — Tier B not justified

**Decision**: Tier B (state-machine PBT with `InMemoryComposition`) is NOT created. The feature's journeys (`read-portfolio-blocked-signals` in `epic-5074-blocked-items.yaml`) have ≥3 chained scenarios, satisfying the first Mandate 10 condition. But the input space is NOT domain-rich in the sense Mandate 10 requires: the blocked transition is a binary state flip (blocked/unblocked) evaluated once per refresh — a single enter/leave event pair, not a rich generative space like emails/dates/payloads/free-text. The state machine is exactly two states with two transitions; modeling it with `RuleBasedStateMachine` adds ceremony without discovering unseen states.

**Rationale**: Mandate 10's Tier B trigger is both conditions: ≥3 chained scenarios AND domain-rich input space. The second condition fails. A feature's blocked spell has exactly 3 observables (featureId, portfolioId, date range), none of which is generative in the PBT sense. Example-based scenarios in Tier A cover the Cartesian product of spell states (open/closed/re-blocked/none) exhaustively.

**Fallback**: If DELIVER discovers unexpected state-space complexity (e.g., concurrent refresh producing interleaved spell open/close), Tier B can be added as a follow-up. The `PortfolioBlockedHistoryAcceptanceTest` base class is already factoring-future-friendly — a Tier B `InMemoryComposition` would share the same step-method vocabulary.

---

## DST-8: OQ-1 resolved — parent features NOT in blocked-eligible set

**Resolution**: `GetBlockedEligibleFeaturesForPortfolio` (`PortfolioMetricsService.cs:270-282`) filters by `f.Portfolios.Any(p => p.Id == portfolio.Id)` — the `Portfolios` navigation populated exclusively by `AddProjectToFeature` at `RefreshFeatures:492`. `RefreshParentFeatures:547-568` calls `AddOrUpdateFeature` but does NOT call `AddProjectToFeature`. Therefore parent features (`IsParentFeature = true`) are NOT in the eligible set. The capture population (visited by `RefreshFeatures`) and the snapshot population (read by `GetBlockedEligibleFeaturesForPortfolio`) agree by construction. ADR-099 guard will not fire from this source.

**Verification**: Pinned by the slice 03 `@property` parity-matrix test (`The_same_spell_shape_answers_identically_on_team_and_portfolio_over_the_same_past_range`) — if parent features DID leak into the snapshot count, the reconstructed membership would diverge and this test would fail.

---

## DST-9: OQ-3 resolved — drill-through passes `blockedSince = null` (parity-as-is)

**Resolution**: `TeamMetricsController:521` passes `null` for `blockedSince` in the historic drill-through (`blockedItemsAtDate`) while `:152` populates it in the historic wip read (`inProgressFeatures`). The portfolio path mirrors this exactly: drill-through answers "which features" — a membership list — not "how long each has been blocked" — a duration read. `blockedSince` in the drill-through is intentionally null on both paths.

**Decision**: No inconsistency to flag. The null `blockedSince` in drill-through is a deliberate simplification — reconstructing each spell's `EnteredAt` per item from the at-date interval scan would add an O(n) lookup to an already O(n) reconstruction. The wip read already serves `blockedSince` for items individually; the drill-through is a batch list for the dialog. Parity-as-is, not a bug.

**No upstream-issues.md entry** for OQ-3 — this is working as designed on both surfaces.

---

## DST-10: Mandate-12 — mechanical criteria met (C# adaptation)

### Criterion 1 — Domain types module

**Status**: MET. C# equivalent is the `FeatureBlockedTransition` entity (`Models/FeatureBlockedTransition.cs`) — the typed record that carries `FeatureId`, `PortfolioId`, `EnteredAt`, `LeftAt`. The scenario/specification split uses typed DTOs from `BlockedItemsJson.cs` and the `JsonElement` API for response parsing. Domain enums: `StateCategories`, `OwnerType`, `WorkTrackingSystems` — all used in test seeding and assertions.

### Criterion 2 — Composition methods consume typed parameters

**Status**: MET. `SeedPortfolio(blockedOnState: bool, blockedState: string)`, `SeedFeature(portfolioId: int, referenceId: string, state: string, stateCategory: StateCategories, ...)`, `DrivePortfolioRefresh(portfolioId: int, connectorFeatures: List<Feature>)` all use typed parameters. No raw-string workarounds where enums exist. `StateCategories` enum is used, not magic strings.

### Criterion 3 — No business logic in step bodies

**Status**: MET. Every specification method in `*Specifications.cs` delegates to the base-class helpers (`SeedPortfolio`, `DrivePortfolioRefresh`, `GetPortfolioWip`, etc.) or to direct JSON assertion helpers (`ItemByReference`, `ReferencesIn`). No control flow in specification methods. AST mechanical: each method body is ≤3 statements — setup call + action call + assertion call. The base class encapsulates all business-logic plumbing (service resolution, DB context, HTTP client configuration).

### Criterion 4 — Step-reuse-ratio (informational)

**Measurement**: Counting `*Specifications.cs` methods unique vs. invocations across all 5 slice scenario files.

| File | Unique methods (Given/When/Then + helpers) |
|---|---|
| Slice01DemoBackfillTeamHistorySpecifications.cs | 12 |
| Slice02FeatureBlockedCaptureSpecifications.cs | 13 |
| Slice03HistoricPortfolioBlockedCountSpecifications.cs | 14 (est.) |
| Slice04PortfolioBlockedDrillThroughSpecifications.cs | 11 (est.) |
| Slice05DemoPortfolioBlockedHistorySpecifications.cs | 15 (est.) |

Step-reuse-ratio (informational): **~1.7×** across the feature. The ratio is naturally capped by the feature shape — each slice has distinct preconditions (demo vs. real, blocked-vs-not, past-vs-today) that do not compress into shared step-methods without degrading the Pillar 1 readability of the scenario names. A config-shaped feature naturally caps below 4×; this is a journey-shaped feature with distinct slice preconditions, and 1.7× is the empirical ceiling.

**Compliance determination**: Criteria 1-3 met. Criterion 4 measured and documented as informational. Mandate-12 PASS.

---

## DST-11: Tier-1 [REF] placement convention — `BlockedItems/` directory

**Decision**: All scenarios and specifications for portfolio-blocked-history live under `Lighthouse.Backend.Tests/API/Integration/BlockedItems/`, alongside the existing epic-5074 blocked-items tests. This follows the repo convention: feature-related integration tests are grouped by business capability (BlockedItems), not by story. The directory already contains 28 files from epic-5074; the 10 new files (5 Scenarios + 5 Specifications for slices 01-05) extend it.

**Rationale**: `tests/API/Integration/{capability}/` is the established pattern — `BlockedItems/`, `Forecast/`, `BlackoutPeriods/`. Test organization conventions skill validates: one directory per business capability, not per feature.

---

## DST-12: E2E is N/A — recorded explicitly

Frontend: confirmed zero changes by C4 / ADR-103. Playwright: no new E2E — existing `BlockedItems.spec.ts` covers shared component behavior. Not a silent skip.

---

## DST-13: No spike/ or devops/ directories — warn + defaults

Per the graceful-degradation matrix: `docs/feature/portfolio-blocked-history/spike/` absent (no spike was run — every mechanism was pre-proven by epic-5074). `docs/feature/portfolio-blocked-history/devops/` absent — using project-default environment matrix (clean, with-pre-commit) per the infrastructure policy. Both WARN-level, not blockers.

---

## Summary — DISTILL decisions

| ID | Decision | Status |
|---|---|---|
| DST-1 | SQLite mandatory, InMemory forbidden for FK-dependent tests | Applied |
| DST-2 | C# scenario/specification split, no .feature files | Applied |
| DST-3 | No PBT framework (FsCheck not in repo) | Applied |
| DST-4 | No E2E (Playwright unchanged, frontend shared) | Applied |
| DST-5 | Frontend N/A (zero work, C4) | Applied |
| DST-6 | Outcomes registry skipped (absent) | Skip recorded |
| DST-7 | Tier A only, Tier B not justified | Applied |
| DST-8 | OQ-1: parent features NOT in eligible set | Resolved |
| DST-9 | OQ-3: drill-through `blockedSince = null` parity-as-is | Resolved |
| DST-10 | Mandate-12: 4 criteria met (C# adaptation) | PASS |
| DST-11 | Test placement: `BlockedItems/` directory | Applied |
| DST-12 | E2E N/A — explicitly recorded | Recorded |
| DST-13 | Missing spike/devops — warn + defaults | WARN |
