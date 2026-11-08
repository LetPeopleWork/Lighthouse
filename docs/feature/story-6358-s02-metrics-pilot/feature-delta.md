# Feature: story-6358-s02-metrics-pilot

Story #6358 · S02 Metrics pilot (go/no-go gate) · Epic #6510 (Modules: package the bounded contexts)

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
- Brief: [`slices/slice-02-s02-metrics-pilot.md`](../epic-6264-modular-monolith/slices/slice-02-s02-metrics-pilot.md)
- ADR: [`adr-270-bounded-contexts-context-map-and-module-layout.md`](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)

DISCUSS: covered at Epic level (see the Epic's Wave Decisions D-6264-3, D-6264-4); waves start at DESIGN.

**Maintainer, 2026-11-08:** hold before DELIVER starts. T9 #6536 counts as finished by the time this lands.

## Wave: DESIGN / [REF] Inherited

Linked, not copied. The catalogue is the single source of truth.

- Catalogue: DDD vocabulary · Story Map (pick-up groups, dependency graph, priority rationale) · How to pick up a
  Story · Stories → S02, S05f, S07b, S12a, S12c · Cross-Cutting Impact Checklist · Outcome KPIs + guardrails ·
  Wave Decisions (D-6264-11: S02's DESIGN decides whether it absorbs S05f and records it; D-6264-16: eleven
  contexts) · Risks 1, 2, 5, 8 · Improvement log (S04a: use `IlDependencies` for dependency ratchets; S05a: the
  `GetAllByPredicate` guard; S01: ADR-270 accepted, S02 settles the folder spelling).
- ADR-270: the Flow metrics context, the context map, the glossary, `Modules/<Context>/{Domain, Application, Adapters}`.
- ADR-268 (ratchet convention: inline file lists, never loosened).

## Wave: DESIGN / [REF] DoR re-run (2026-11-08)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Metrics is a type-set rule with no namespace (`MetricsArchitectureTests.cs`), and the two services hold 27 `GetAllByPredicate` calls. |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | Real files below (re-count) and the S05a allow-list entries. |
| 4 UAT | PASS (catalogue) | Four testable AC in the brief, CI-observable; Given/When/Then scenarios are written at DISTILL. |
| 5 AC from UAT | PASS | AC map to the brief's Elevator Pitch "After". |
| 6 Right-sized | PASS by waiver | Size L; DESIGN cuts it into slices of a day or less (D-6264-4). |
| 7 Technical notes and cross-cutting | PASS | Catalogue checklist; re-answered for this Story at DESIGN. |
| 8 Dependencies | PASS | Hard: S01 #6357 Closed. Soft: S05a #6369 Closed; S12c #6388 New (Metrics DTOs stay in `Models`, out of scope); S05f #6374 New (absorb or not is decided at DESIGN); S07b #6378 and S12a #6386 New, out of scope. |
| 9 Outcome KPIs | PASS | KPIs 1, 2, 4, 12 from the catalogue. |

## Wave: DESIGN / [REF] Baseline re-count (old → new, 2026-11-08)

| Measure | Brief / catalogue | Today |
|---|---|---|
| `GetAllByPredicate` calls in the two metrics services | 27 | **27** (Team 14, Portfolio 13; `BaseMetricsService` 0) |
| S05a `GetAllByPredicate` allow-list (KPI 4) | 15 files at landing | **15** files; Metrics-related: `TeamMetricsService`, `PortfolioMetricsService`, `BlockedCountSeriesBuilder`, `BackgroundServices/OverTimeFillTarget`, `DomainEvents/DemoBlockedHistoryBackfillHandler`, `DomainEvents/DemoPercentilesBackfillHandler` (the last three: membership decided at DESIGN) |
| Loose files in `Services/Interfaces` + `Services/Implementation` (KPI 1) | 42 + 58 top level; 507 total (2026-10-22) → 41/61 (S01) | **41 + 61** top level; **527** total |
| `Models` files (KPI 3) | 254 (S01) | **261** |
| Contexts packaged with namespace rules (KPI 2) | 0 of 9 → 0 of 11 (ADR-270) | **0 of 11**; `Modules/` does not exist yet |
| Metrics files flat in `Services/Implementation` | brief's list | 17: `BaseMetricsService`, `TeamMetricsService`, `PortfolioMetricsService`, `PercentileCalculator`, `PercentileFamilies`, `XmRCalculator`, `SleRiskCalculator`, `BaselineValidationService`, `BlockedCountSeriesBuilder`, `MetricsCacheKey`, `PercentileSnapshotWriter`, `ProcessBehaviorSnapshotWriter`, `PercentilesOverTimeSeriesQuery`, `ProcessBehaviorSeriesQuery`, `GapAsking*SeriesQuery` (2), `DeliveryMetricValuesProjector` (context to be confirmed against ADR-270) |
| Metrics interfaces in `Services/Interfaces` | — | 6: `ITeamMetricsService`, `IPortfolioMetricsService`, `IPercentileSnapshotWriter`, `IPercentilesOverTimeSeriesQuery`, `IProcessBehaviorSeriesQuery`, `IProcessBehaviorSnapshotWriter` |
| Metrics type-set rule | "a type-set rule today" | `Lighthouse.Backend.Tests/Architecture/MetricsArchitectureTests.cs` |
| S107 suppressions in Metrics (KPI 6) | — | `TeamMetricsService` (plus both Metrics controllers, which are S12a's) |

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels and topologies are unchanged. The existing API, E2E and response tests stay green unmodified, apart from tests that move along with their code.
- [ ] **Ratchet in the same change**: the namespace-based Metrics rule replaces the type-set rule in the same commit range; baselines are inline file lists; no rule loosened.
- [ ] **Moves separate from edits**: `refactor(metrics): move …` commits apart from edit commits.
- [ ] **Re-count before designing**: done above; DESIGN confirms the membership against ADR-270.
- [ ] **Mutation last, on frozen code**: the Metrics score is not lower.
- [ ] **Architecture docs in the same change**: ADR-270 amended (lessons + go / go-with-changes / stop), `ARCHITECTURE.md` §4 Metrics row (and §6 if read ports land).
- [ ] **Payoff measured where claimed**: N/A unless DESIGN claims a performance payoff for the read ports.
- [ ] **No user-facing release notes** unless the maintainer says so.
- [ ] **One context at a time**: old and new layouts coexist.
- [ ] **Terminology**: names follow ADR-270's glossary.
- [ ] **Cross-cutting re-check**: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, each answered with no silent N/A.
- [ ] **S05f decision recorded**: absorbed or kept, with the ADO change shown to the maintainer.
- [ ] **Read ports proven on a provider-backed test** (EF InMemory translates nothing).
- [ ] **`IlDependencies`** for the new dependency rules.
- [ ] **ADR number from `origin/main`** at write time, if an ADR is added rather than ADR-270 amended.
