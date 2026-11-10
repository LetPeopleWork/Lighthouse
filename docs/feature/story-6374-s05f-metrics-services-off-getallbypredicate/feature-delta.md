# Feature: story-6374-s05f-metrics-services-off-getallbypredicate

Story #6374 · S05f Move the two metrics services off `GetAllByPredicate` · Epic #6507 (Narrow persistence access:
repositories by role)

- Catalogue: [`docs/feature/epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md)
- Brief: [`slices/slice-18-s05f-metrics-services-off-getallbypredicate.md`](../epic-6264-modular-monolith/slices/slice-18-s05f-metrics-services-off-getallbypredicate.md)
- ADR: [`adr-270-bounded-contexts-context-map-and-module-layout.md`](../../product/architecture/adr-270-bounded-contexts-context-map-and-module-layout.md)

DISCUSS: covered at Epic level (see the Epic's Wave Decisions D-6264-3, D-6264-4); waves start at DESIGN.

**Maintainer, 2026-11-10:** picked S05f as the next improvement item, ahead of the first S03*.

## Wave: DESIGN / [REF] Inherited

Linked, not copied. The catalogue is the single source of truth.

- Catalogue: DDD vocabulary · Story Map (dependency graph `S05a ==> S05b..S05f ==> S05g`, priority rationale) · How to
  pick up a Story · Stories → S05f, S05b, S05g, S07b, S12a · Cross-Cutting Impact Checklist · Outcome KPIs +
  guardrails (KPI 4, KPI 12) · Wave Decisions (D-6264-11: S05f kept separate, decided at S02's DESIGN) · Risks 2, 4,
  5, 6 · Improvement log (S05a: the `GetAllByPredicate` guard and `InlineBaseline`; S04a: `IlDependencies`; S02: the
  Flow metrics module, its rules R2–R5 and `RepositoryRecognition`, and the lessons in ADR-270).
- ADR-270: the Flow metrics module layout, `Modules/FlowMetrics/{Domain, Application, Adapters}`; "Lessons from the
  Flow metrics pilot".
- ADR-268 (ratchet convention: inline file lists, never loosened). ADR-163 (repository conventions; S05g generalises it).
- S02's open question 4: `TeamMetricsService.UpdateTeamMetrics` writes through `IWorkItemRepository.Save`.

## Wave: DESIGN / [REF] DoR re-run (2026-11-10)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | 27 `GetAllByPredicate` calls in the two metrics services. EF InMemory translates nothing, so a predicate SQLite or Postgres cannot translate still passes in tests. |
| 2 Persona | PASS | `lighthouse-maintainer` plus the CI build; `@infrastructure`. |
| 3 Domain examples | PASS | The real call sites in the re-count below. |
| 4 UAT | PASS (catalogue) | Two AC in the catalogue, three in the brief, all CI-observable. |
| 5 AC from UAT | PASS | AC map to the brief's Elevator Pitch "After". |
| 6 Right-sized | PASS by waiver | Size M; DESIGN cuts it into slices of a day or less (D-6264-4), Team first, then Portfolio. |
| 7 Technical notes and cross-cutting | PASS | Catalogue checklist; re-answered for this Story at DESIGN. |
| 8 Dependencies | PASS | Hard: S05a #6369 Closed. Soft: S02 #6358 Closed (kept S05f separate); **S05b #6370 New** (verify in-memory filtering via `Func` predicates), warned, not blocking. Out of scope: S07b #6378, S12a #6386. |
| 9 Outcome KPIs | PASS | KPI 4 (`GetAllByPredicate` allow-list), KPI 12 (mutation). |

## Wave: DESIGN / [REF] Baseline re-count (old → new, 2026-11-10)

| Measure | Brief / catalogue | Today |
|---|---|---|
| `GetAllByPredicate` calls in the two metrics services | 27 (Team 14, Portfolio 13) | **27** (Team 14, Portfolio 13); now in `Modules/FlowMetrics/Application/` |
| S05a allow-list (`RepositoryLeakRatchetArchUnitTest.FilesThatCallGetAllByPredicate`, KPI 4) | 15 files | **15** files; Flow metrics entries: the two services, `BlockedCountSeriesBuilder` (1 call), `OverTimeFillTarget` (2), `Policies/DemoBlockedHistoryBackfillHandler` (2), `Policies/DemoPercentilesBackfillHandler` (2); plus `API/TeamMetricsController` (1, S12a's) |
| Calls outside the persistence adapters | 52 | **55** (outside `Data`, `Repositories` and `Adapters/Persistence`) |
| Hypothesis: 27 calls collapse to ~10 named queries | — | Shapes seen at re-count: work items of a team in a set of state categories (5 category sets, one counted), all items of a team, transitions for item ids (Team ×2, Portfolio ×3), features of a portfolio in a set of state categories (4 sets), all features of a portfolio, features sized by a team's work (`.Any()` per Feature). Looks like **≤ 10**; DESIGN confirms. |
| `GetAllByPredicate` return type | — | `IQueryable<T>` (`IRepository.cs:18`); `WorkItemRepository` and `FeatureRepository` override it (includes) |
| Flow metrics mutation (S02 slice 8) | — | 64.31 %; `TeamMetricsService` 30.58 %, `PortfolioMetricsService` 27.60 % |

## Wave: DESIGN / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels and topologies are unchanged. The existing API, E2E and response tests stay green unmodified, apart from tests that move along with their code.
- [ ] **Ratchet in the same change**: both services leave the S05a allow-list in the commit range that makes them pass; inline file list; no rule loosened.
- [ ] **Moves separate from edits**: move or rename commits apart from edit commits.
- [x] **Re-count before designing**: done above.
- [ ] **Mutation last, on frozen code**: no touched file lower than its baseline (S02's bar for a behaviour-free change).
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §6 (read-side ports) and ADR-270 if the Flow metrics ports change its picture; done in the finalize checklist, not in a slice.
- [ ] **Payoff measured where claimed**: N/A unless DESIGN claims a performance payoff for the read ports.
- [ ] **No user-facing release notes** unless the maintainer says so.
- [ ] **One context at a time**: only Flow metrics reads change.
- [ ] **Terminology**: names follow ADR-270's glossary.
- [ ] **Cross-cutting re-check**: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, each answered with no silent N/A.
- [ ] **Read ports proven on a provider-backed test** (EF InMemory translates nothing), `.Any()` per Feature included.
- [ ] **Flow metrics module rules hold**: ports in `Application`, EF implementations in `Adapters/Persistence` (R5: no EF Core or `Data` in Application).
- [ ] **EXAMINE commands declared up front** (reuse `Scripts/examine`; the metrics answer comparison applies).
- [ ] **ADR number from `origin/main`** at write time, if an ADR is added.
