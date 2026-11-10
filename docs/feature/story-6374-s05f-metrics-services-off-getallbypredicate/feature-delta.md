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
- [x] **EXAMINE commands declared up front** (reuse `Scripts/examine`; the metrics answer comparison applies).
- [ ] **ADR number from `origin/main`** at write time, if an ADR is added.

Note on the checklist item "ports in `Application`, EF implementations in `Adapters/Persistence`": DESIGN recommends
against that placement (D-3) and asks the maintainer (open question 1). If D-3 a is accepted, that item reads "R5 holds,
no module rule baseline changes" instead.

## Wave: DESIGN / [REF] Context-map placement (2026-11-10)

- **Contexts touched**: Flow metrics (the consumer; only its reads change). The owner contracts it reads through get new
  named reads: Work Items (work items, their state transitions, Features as items of the upper level) and Organisation
  set-up (Portfolio membership, `Feature.Portfolios`), both still in `Services/Interfaces/Repositories` until S03b/S03i.
- **New terms / concepts**: none in the glossary. Method names use the state-category words already in the code
  (`ToDo`, `Doing`, `Done`); no new domain concept, no value object (D-2).
- **Domain events**: publishes none, reacts to none differently.
- **UI surface**: none.
- **ADR-270 / `ARCHITECTURE.md` edits**: "SSOT edits pending"; done in the finalize checklist, not in a slice (S02 lesson 3).
- **Improvement backlog check**: this Story is itself an improvement item (child of #6507), not a new feature Epic: N/A, stated.

## Wave: DESIGN / [REF] Evidence from the design-time measurements (2026-11-10, ROOT = this worktree)

Code facts: `code-fact` is refused by this session's shell allowlist (exit 126), so every fact below comes from Grep and
Read over the worktree (lexical, `noisy` grade). Limits: a call written through reflection or a delegate would be missed;
none was seen.

| # | Finding | Consequence |
|---|---|---|
| E1 | `WorkItemRepository.GetAllByPredicate` = `Context.WorkItems.Include(Team).Where(p)`, **deferred and tracked**. | A named read keeps `Include(Team)`, tracking, and executes once. |
| E2 | `FeatureRepository.GetAllByPredicate` = `featureOrdering.Order(GetFeatures().Where(p).AsEnumerable()).AsQueryable()`: **executes at once**, tracked, includes Portfolios, FeatureWork→Team, Forecasts→SimulationResults, StartForecasts→SimulationResults, DependsOnReferences, then orders **in memory** by the instance's ordering policy, ties on Id. Anything chained after it runs in LINQ to Objects. | A named Feature read must be exactly `Order(GetFeatures().Where(p))`. The existing `FeaturesOfPortfolio` / `FeaturesOfTeam` (`ListRead`) are **not** that read: no tracking, and only the Feature's own start forecast. Reusing them changes answers (D-4). |
| E3 | The two state-transition repositories add nothing to `RepositoryBase` (no include, no order). | Named transition reads are a plain `Where(ids.Contains(...))`, enumerated once. |
| E4 | Every deferred work-item query is enumerated **once** by its consumer: `GenerateThroughputRunChart` / `GenerateStartedRunChart` iterate once; the two `Generate*ByDay` calls at Team 640 and 708 are single-day; the rest call `.ToList()`. | Materialising inside the repository adds no statement. `RealityCheckQueryCountTest` (real SQLite, real `WorkItemRepository`, counts statements: 20 and 24) pins it, unmodified. |
| E5 | Team 81–83 `.Count()` runs server-side (`SELECT COUNT`) on the deferred query. | The named count read stays a server-side `Count()`, never load-and-count. |
| E6 | Team 653: the size chart's `Where(Done ∨ ToDo ∨ Doing)` runs **in memory** on the already-ordered Feature list (E2), as the comment at 688 says. | The filter stays in the service, in memory; it is not pushed into SQL (D-4, F7). |
| E7 | The row order of every work-item and transition read is **unspecified** (no `ORDER BY`), and it is **visible**: per-day item lists are serialised in `RunChartDataDto.WorkItemsPerUnitOfTime`. | Each predicate keeps its SQL (D-2); the parity tests compare sequences, not sets. |
| E8 | Every predicate uses mapped columns (`TeamId`, `StateCategory` enum stored as int, the `Portfolios` skip navigation, `FeatureWork.TeamId/TotalWorkItems`, `IsUsingDefaultFeatureSize`); `onlyIn == null \|\| …` captures a nullable enum (becomes a parameter); `Contains` is over a captured `HashSet<int>` (EF Core 10.0.14, Npgsql 10.0.3). S02's SQLite capture ran these reads on real data and got answers. | **No predicate found that cannot be translated today**, so no behaviour question to raise. Postgres has no evidence in the repository: the parity tests supply it. |
| E9 | The metrics unit tests stub `GetAllByPredicate(It.IsAny<Expression<…>>())` and return one list **whatever the predicate** (Team: `TeamMetricsServiceTests`, `TeamMetricsServiceSnapshotTests`, `WorkItemAgeAsOfDateTest`, `MetricsAsOfGroupNameTest`, `InstanceDayItemBucketingTest`, `ClosedInRangeCharacterisationTest`; Portfolio: `PortfolioMetricsServiceTests` with 12 set-ups, `FeatureRepositoryDoubles`, and the shared classes). 11 test files build a service with `new`. | "Existing tests unmodified" cannot hold literally: their **Arrange** lines retarget to the named reads; their **Assert** lines must not change (D-6). A loose Moq default returns an empty list for a read nobody set up, so a missed set-up turns a test red, not falsely green. |
| E10 | `PortfolioMetricsService` takes `IRepository<Feature>`; DI forwards it to `IFeatureRepository` (Program.cs 1355). | It takes `IFeatureRepository` instead: the same instance, no Program.cs edit. |
| E11 | Ratchet: the services leave `FilesThatCallGetAllByPredicate` (15 → 14 → 13, `Has.Length.EqualTo`). R3/R4/R5 (`FlowMetricsInbound*`, `FlowMetricsApplicationOutbound`) do not change: no edge is added (D-3). | No other baseline is edited; ADR-270's gate change 3 ("lift the Application rules in the next Story that touches them") is not triggered. |
| E12 | Provider-test precedent: `Integration/Containers/FeatureListParityOnPostgresTests` (SQLite file plus `SharedPostgresContainer`, on `IntegrationFixture`), not on the time-budget allow-list (backend class budget 34 s). | The parity tests reuse its shape (D-5). |

### Hypothesis measurement: 27 calls → 13 named reads (confirmed)

| Named read (owner contract) | Shape | Call sites (file:line) |
|---|---|---|
| W1 `DoneItemsOfTeam(teamId)` (`IWorkItemRepository`) | TeamId ∧ Done | Team 142, 914 |
| W2 `DoingOrDoneItemsOfTeam(teamId)` | TeamId ∧ (Doing ∨ Done) | Team 229 (written Done ∨ Doing), 266, 279, 636, 704 |
| W3 `ToDoOrDoingItemsOfTeam(teamId)` | TeamId ∧ (ToDo ∨ Doing) | Team 760 |
| W4 `CountToDoOrDoingItemsOfTeam(teamId)` | same, server-side `COUNT` | Team 81–83 |
| W5 `ItemsOfTeam(teamId)` | TeamId | Team 251, 529 |
| T1 `TransitionsOfWorkItems(ids)` (`IWorkItemStateTransitionRepository`) | ids.Contains(WorkItemId) | Team 542, 605 |
| F1 `FeaturesWithRealWorkForTeam(teamId, onlyIn?)` (`IFeatureRepository`) | (onlyIn) ∧ ¬default size ∧ FeatureWork.Any(team ∧ total > 0) | Team 693 (two callers, 652 and 666) |
| F2 `DoneFeaturesOfPortfolio(portfolioId)` | Portfolios.Any(id) ∧ Done | Portfolio 28, 755 |
| F3 `DoingOrDoneFeaturesOfPortfolio(portfolioId)` | Portfolios.Any ∧ (Doing ∨ Done) | Portfolio 143, 161, 182 (written Done ∨ Doing), 216, 599 |
| F4 `ToDoOrDoingFeaturesOfPortfolio(portfolioId)` | Portfolios.Any ∧ (ToDo ∨ Doing) | Portfolio 264 |
| F5 `ToDoDoingOrDoneFeaturesOfPortfolio(portfolioId)` | Portfolios.Any ∧ (Done ∨ ToDo ∨ Doing) | Portfolio 564 |
| F6 `EveryFeatureOfPortfolio(portfolioId)` | Portfolios.Any | Portfolio 434 |
| T2 `TransitionsOfFeatures(ids)` (`IFeatureStateTransitionRepository`) | ids.Contains(FeatureId) | Portfolio 241, 447, 504 |

13 reads for 27 calls (7 for Team, 6 for Portfolio; it would be 6 in all if the category set were a parameter, D-2).
The hypothesis holds: named repository reads, not a query object per capability. The names are working names; the
crafter may improve them, but F6 must not be called `FeaturesOfPortfolio`, which already names the list reader's
different read. Not in scope, and not a `GetAllByPredicate` call: Team 85 `featureRepository.GetAll()` (in memory, E2)
and `InProgressFeatureResolution` (already a named read).

## Wave: DESIGN / [REF] Decisions

Status: all **RECOMMENDED**, for the maintainer at the DESIGN hold.

### D-1 Read model — **RECOMMENDED: named repository reads** (hypothesis confirmed, table above)

A query object per capability (S12a pattern) is rejected: 27 calls collapse 2:1, and the shapes are reads of an owner's
data, not capabilities.

### D-2 Shape of a read — **RECOMMENDED: option b**

| Option | Detail | Verdict |
|---|---|---|
| a | One read per owner with the category set as a parameter (`ItemsOfTeam(teamId, params StateCategories[])`), implemented as `set.Contains(StateCategory)` | 6 methods, reusable by the S05g callers. Changes the SQL (`IN` over a parameter collection instead of `OR`); the row order is unspecified and visible (E7), so a new plan may reorder the per-day item lists. Rejected for a behaviour-free Story. |
| **b** | One named read per category set, each keeping today's predicate literally (one private helper per repository holds the shared include and ordering code) | 13 one-line methods; the same SQL up to parameter names, so the same plan and the same order. |
| c | A `StateCategorySet` value object with named instances | A type with no other consumer (YAGNI), and the same SQL risk as a. Rejected. |

Why b: "the same SQL" is the only behaviour-free claim that holds without a new ordering argument. Weakest point: 13
methods on interfaces that are already wide; S05g or S03b can fold them into option a once order is made explicit.

### D-3 Where the ports and implementations live — **RECOMMENDED: option a**

| Option | Detail | Verdict |
|---|---|---|
| **a** | Named reads on the owner contracts `IWorkItemRepository`, `IFeatureRepository`, `IWorkItemStateTransitionRepository`, `IFeatureStateTransitionRepository`, implemented in the existing repositories | ADR-270 Decision 8: "reading goes through the owner's contract". Reuses the owner's one definition of how a Feature is read whole (E2). No Program.cs edit (so no forced full CI run), no new edge, R3/R4/R5 unchanged. Team keeps its constructor; Portfolio's changes `IRepository<Feature>` → `IFeatureRepository` (E10). |
| b | Flow-metrics read ports in `Modules/FlowMetrics/Application`, EF implementations in `Modules/FlowMetrics/Adapters/Persistence` | Satisfies R5, but the adapter reads Work Items' and Organisation set-up's tables directly (against Decision 8) and copies `GetFeatures()`'s includes and the ordering, a second authority for "a Feature read whole"; or it calls `GetAllByPredicate` from inside the exempt folder, which only hides the call from the S05a scan. Adds constructor parameters and Program.cs registrations. Rejected. |
| c | Segregated owner-side readers (`IWorkItemReader`, like `IFeatureListReader`) | Cleaner interface segregation; needs Program.cs forwards and constructor changes. An extract-interface on top of a, for S05g or S03b. Deferred. |
| c′ | Flow-metrics ports implemented by the `Services` repositories | `WorkItemRepository` would reach the module's Application: a newcomer on R4's list, a baseline that grows. Forbidden. |

Return types: `IReadOnlyList<T>` (`int` for W4). No `IQueryable`, `Expression` or `Func` in a new signature (the S05c–e AC).

### D-4 Behaviour-free guarantee of each read — **RECOMMENDED**

- Work items: `Context.WorkItems.Include(wi => wi.Team).Where(<today's predicate>).ToList()`, tracked. W4: `Count()` on the query.
- Features: `featureOrdering.Order(GetFeatures().Where(<today's predicate>).AsEnumerable()).ToList()`, tracked, all five includes. Never `ListRead`.
- Transitions: `Where(ids.Contains(...)).ToList()`, ids typed `IReadOnlyCollection<int>` (the services pass their `HashSet<int>`).
- Team size chart: F1 with `onlyIn: null`; the `Done ∨ ToDo ∨ Doing` filter stays in the service, in memory (E6), so the in-memory ordering sees exactly today's set.
- Tracking stays on: identity, and the `Save` in `UpdateTeamMetrics`, see the same instances. `AsNoTracking` is a separate, measured change, not this Story.
- No place found where a current predicate cannot be translated (E8): nothing to raise as a behaviour question.

### D-5 Provider-backed proof — **RECOMMENDED: parity tests on SQLite and Postgres**

One parity file per nwdes feature, in `Lighthouse.Backend.Tests/Integration/Containers/`, on `IntegrationFixture`,
shaped like `FeatureListParityOnPostgresTests`. Given: two Teams (resp. Portfolios) with items or Features in all four
categories (Unknown included), a Feature in two Portfolios, a Feature on a default size, FeatureWork with total 0 and
total above 0, transitions for several ids. Then, on a SQLite file and on `SharedPostgresContainer`, each named read
returns **the same ids in the same sequence** as today's predicate run through the base query, with its graph loaded
(W: `Team` is set; F: Portfolios, FeatureWork with its Team, Forecasts, StartForecasts per Team, DependsOnReferences),
and W4 equals the length of W3. Speed tier: Integration (not in `Test-Fast.ps1`), needs Docker; budget **34 s per
class** (the backend default), one container per class; no allow-list entry and no exception requested. If a class
measures over half its budget, timing evidence (nine runs) is owed before asking for one.

### D-6 Behaviour-free oracle — **RECOMMENDED**

1. Every existing metrics test keeps its **Assert** lines byte-identical; only the **Arrange** lines that stubbed
   `GetAllByPredicate` retarget to the named reads, through one test helper per service ("every read of this Team
   returns this list"), so a test keeps today's any-predicate semantics in one line (E9). Reviewer check: the `git diff`
   of those files shows no changed `Assert`, `Is.` or `Verify` line, with one stated exception:
   `TeamMetricsServiceTests:194` verifies `GetAllByPredicate` was never called on Features; it retargets to F1 with the
   same `Times.Never`.
2. `RealityCheckQueryCountTest` stays green with its assertion unchanged (statement count, E4).
3. The S02 answer comparison, base = the S05f base revision (`a86e3f8b4`, or the DESIGN commit), candidate = the head of each nwdes feature.
4. EXAMINE commands, declared now (`.nwave/examine.json` unchanged):
   - `Scripts/examine/bin/architecture-rules RepositoryLeakRatchet` (S05a list: 14, then 13 entries)
   - `Scripts/examine/bin/architecture-rules FlowMetrics Release` (module rules unchanged, in CI's configuration)
   - `Scripts/examine/bin/metrics-answers-compare <S05f-base> <candidate>` (exit 0, "identical")
   - `Scripts/examine/bin/backend-tests ParityOnBothProviders` and `Scripts/examine/bin/backend-tests` (the whole filtered suite)
   - `Scripts/examine/bin/ef-model-unchanged` (no model change expected; a cheap guard)
   The examiner must pass the base revision explicitly: the script's default base is S02's (`7ec328cb6`).

### D-7 Ratchet scope — **RECOMMENDED: the two services only; no new rule**

`FilesThatCallGetAllByPredicate` 15 → 14 (Team) → 13 (Portfolio), inline, `Has.Length` updated in the same commit.
`BlockedCountSeriesBuilder`, `OverTimeFillTarget` and the two demo backfill handlers stay for S05g, as the brief says:
they read the module's own snapshot tables or other shapes, which the reads above do not cover. A new rule "Flow
metrics Application does not depend on `IRepository<T>`" is rejected for now: 8 policies and `OverTimeFillTarget` still
use `IRepository<Team>` / `IRepository<Portfolio>` (S05c/d), so it would start with a 9-entry list, and it would touch
the module rule files, which by ADR-270's gate change 3 forces the shared-class lift first. The tightening that comes
free: `PortfolioMetricsService` no longer names `IRepository<Feature>`.

### D-8 S02's open question 4 (`UpdateTeamMetrics` → `IWorkItemRepository.Save`) — **RECOMMENDED: leave it**

It is a write (it flushes the Team's refresh time and Feature WIP on the shared context), not a read. Moving it changes
unit-of-work behaviour, and it is S07b's (composition) or S03b's (Work Items as owner) decision. Recorded, not taken.

### D-9 Mutation bar — **RECOMMENDED**

Stryker on frozen code at the end of each nwdes feature: `TeamMetricsService` ≥ 30.58 %, `PortfolioMetricsService`
≥ 27.60 % (no touched file lower than its baseline); the new read methods ≥ 80 %, killed by the parity tests, so the
config's `test-case-filter` must name the parity classes (they sit outside `API.Integration` but carry the
`Integration` category: check that the filter keeps them). Killing the services' old survivors is a separate
improvement item, not this one.

### D-10 Slicing and RED — **RECOMMENDED: two nwdes features, one value slice each, with a declared prefactoring move**

A behaviour-free extraction has one semantic RED per service: delete the service's line from the S05a list (and the
count) while it still calls `GetAllByPredicate`; the guard fails and names the file. A parity test of a read that does
not exist yet fails only by not compiling, which nwdes does not accept as RED.

| Option | Shape | Verdict |
|---|---|---|
| **a** | Feature A (Team), feature B (Portfolio). Each has one value slice whose oracle is the S05a list edit, plus its own parity file; before it, a declared **prefactoring** move (add the named reads, no caller yet) verified by the existing GREEN suite before and after the move, with the parity file landing in it | Every RED is semantic; the S05a file is edited once per nwdes feature, so no oracle file is shared across slices of one feature (S02 lesson 2). Weakest point: Team's slice plus its prefactoring is about 1.25 days. |
| b | Two slices per service, the first with only a parity file | The first slice has no semantic RED. |
| c | A per-slice call-count oracle file (`TeamMetricsService.cs` has at most 3 calls) | Ceremony that repeats the S05a scan. |

### D-11 Formal verification — **RECOMMENDED: no claim is load-bearing for a checker; a waiver for the maintainer to confirm**

Claims: (1) each named read returns the same rows, in the same order, with the same loaded graph as today's predicate,
on SQLite and on Postgres; (2) no extra statements; (3) the S05a list shrinks. None is a state-transition, concurrency,
liveness or recovery claim. (1) is an equivalence across two database engines and EF's translation: a Quint or TLA+
model could only restate it, and an Agda proof would be about a model of LINQ, not about EF's translation. The evidence
is executable: the provider parity tests, the query-count test and the answer capture. Checker probe: `quint
--version`, `java -version` and `agda --version` were **refused by this session's shell allowlist** (exit 126), so
availability is INDETERMINATE, not absent. Proof status: INDETERMINATE for every claim, under a waiver the maintainer
must confirm at the DESIGN hold (reason: no modelable state machine; native provider tests carry the evidence). If the
maintainer wants a checker instead: `npm install -g @informalsystems/quint` (Node on this Windows host is unverified),
after allowing `quint` in the shell allowlist.

## Wave: DESIGN / [REF] Component decomposition

Paths relative to `Lighthouse.Backend/Lighthouse.Backend/`. NEW member / EDIT / STAY.

| Type | Change |
|---|---|
| `Services/Interfaces/Repositories/IWorkItemRepository` | EDIT: W1–W5 |
| `Services/Implementation/Repositories/WorkItemRepository` | EDIT: W1–W5 and a private helper |
| `IWorkItemStateTransitionRepository` / `WorkItemStateTransitionRepository` | EDIT: T1 |
| `IFeatureRepository` / `FeatureRepository` | EDIT: F1–F6 and a private helper over `GetFeatures()` |
| `IFeatureStateTransitionRepository` / `FeatureStateTransitionRepository` | EDIT: T2 |
| `Modules/FlowMetrics/Application/TeamMetricsService` | EDIT: 14 call sites; constructor unchanged |
| `Modules/FlowMetrics/Application/PortfolioMetricsService` | EDIT: 13 call sites; `IRepository<Feature>` → `IFeatureRepository` |
| `BaseMetricsService`, controllers, Program.cs, DTOs, EF model | STAY |
| Tests: `RepositoryLeakRatchetArchUnitTest` | EDIT: one line and the count, once per nwdes feature |
| Tests: two parity files under `Integration/Containers/` | NEW |
| Tests: about 11 metrics test files, `FeatureRepositoryDoubles` | EDIT, Arrange lines only (D-6) |

## Wave: DESIGN / [REF] Driving ports

Unchanged: the three metrics controllers, the refresh-event policies, the hosted filler, Forecasting's calls into
`ITeamMetricsService`. Neither `ITeamMetricsService` nor `IPortfolioMetricsService` changes.

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter | Status |
|---|---|---|
| `IWorkItemRepository` (+W1–W5), `IWorkItemStateTransitionRepository` (+T1), `IFeatureRepository` (+F1–F6), `IFeatureStateTransitionRepository` (+T2) | Platform EF repositories over `LighthouseAppContext` | modified (new read members) |
| `GetAllByPredicate` | same | unchanged; S05g makes it `protected` |
| Snapshot repositories, `IBlackoutPeriodService`, `IForecastFilterRuleService`, `ILighthouseClock` | unchanged | unchanged |

No new external call, no tracker call. Failure translation unchanged: a provider exception propagates as it does today.

## Wave: DESIGN / [REF] Technology choices

None new. EF Core 10.0.14, Npgsql 10.0.3, Testcontainers.PostgreSql 4.16.0, NUnit, Moq and Stryker.NET are all present.

## Wave: DESIGN / [REF] Reuse Analysis

| Candidate | Overlap | Decision |
|---|---|---|
| `FeatureRepository.GetFeatures()` + `IFeatureOrdering` | How a Feature is read whole and ordered | **REUSE** inside F1–F6 |
| `IFeatureListReader.FeaturesOfPortfolio` / `FeaturesOfTeam` | Same filter as F6, close to F1 | **Rejected**: a different graph and no tracking (E2); a coincidental similarity, not the same fact |
| `FeatureRepository.FeaturesWorkedOnBy(teamIds)` | FeatureWork.Any(team) | **Rejected**: no `TotalWorkItems > 0`, no default-size exclusion, no simulation results |
| `WorkItemBlockedTransitionRepository.GetWorkItemIdsWithBlockedHistory` | `ids.Contains` shape | A different table; the pattern only |
| Flow metrics `Adapters/Persistence` | Module persistence | Rejected for these reads (D-3) |
| `FeatureListParityOnPostgresTests`, `SharedPostgresContainer`, `IntegrationFixture` | Provider parity | **REUSE** the shape |
| `RepositoryLeakRatchetArchUnitTest`, `InlineBaseline` | Ratchet | **EXTEND** (shrink the list) |
| `Scripts/examine/*`, S02's capture | Behaviour comparison | **REUSE**, with an explicit base revision |
| `RealityCheckQueryCountTest` | Statement count | **REUSE**, unmodified |

## Wave: DESIGN / [REF] Ratchet design

| # | Rule | Change |
|---|---|---|
| S05a | `FilesThatCallGetAllByPredicate` | 15 → 14 (feature A) → 13 (feature B); `Has.Length` in the same commit |
| S05a sibling | `ApiTypesThatDependOnARepository` (23) | unchanged |
| R2–R5, R6 | Flow metrics module rules, handler order | unchanged (no edge added or removed; checked with `architecture-rules FlowMetrics Release`) |
| new | none (D-7) | — |

## Wave: DESIGN / [REF] Slices

Two nwdes features, Team first. Moves separate from edits: the prefactoring commit adds reads with no caller; the slice
commit switches the callers. Mutation and docs go in the finalize checklist, not in slices.

| Id | Kind | Delivers | Oracle (own file) | Production targets | Size |
|---|---|---|---|---|---|
| A-P | prefactoring of A-S1 | W1–W5, T1, F1 on the owner repositories, no caller yet | parity file `Integration/Containers/TeamMetricsReadsParityOnBothProvidersTests.cs` (support, proven by a deliberate break) + the existing suite GREEN before and after | the three repository interfaces and classes | 0.5 d |
| A-S1 | value | `TeamMetricsService` reads only through named reads and leaves the S05a list (15 → 14) | `RepositoryLeakRatchetArchUnitTest` list edit; RED: the guard names `TeamMetricsService.cs` as a caller not on the list | `TeamMetricsService.cs`; Arrange lines of the Team metrics tests | 0.75 d |
| B-P | prefactoring of B-S1 | F2–F6, T2, no caller yet | parity file `Integration/Containers/PortfolioMetricsReadsParityOnBothProvidersTests.cs` + the existing suite GREEN | `IFeatureRepository` / `FeatureRepository`, the feature-transition repository | 0.5 d |
| B-S1 | value | `PortfolioMetricsService` reads only through named reads, takes `IFeatureRepository`, and leaves the S05a list (14 → 13) | the same list edit; RED names `PortfolioMetricsService.cs` | `PortfolioMetricsService.cs`; Arrange lines of the Portfolio tests, `FeatureRepositoryDoubles` | 0.75 d |

Verification commands (each slice): `pwsh -NoProfile -File Lighthouse.Backend/Test-Fast.ps1` (fast loop);
`dotnet test Lighthouse.Backend/Lighthouse.Backend.Tests --filter "FullyQualifiedName~RepositoryLeakRatchetArchUnitTest|FullyQualifiedName~ParityOnBothProviders|FullyQualifiedName~RealityCheckQueryCountTest"`
(the slice's oracle and parity tests; Docker needed); at the end of the slice, `dotnet build` (zero warnings) and the
filtered `dotnet test` from CLAUDE.md.

Falsifiers: put one `GetAllByPredicate` call back → the S05a guard goes red and names the line; drop `Doing` from W2,
or the `StartForecasts` include from F3 → the parity test goes red on both providers; make W1 read twice →
`RealityCheckQueryCountTest` goes red; make W4 load the rows and count them in memory → no test sees it (same number),
so it is a review item.

## Wave: DESIGN / [REF] Self-falsification (concrete counterexamples, 2026-11-10)

| # | Attack | What it showed |
|---|---|---|
| F1 | A W-read without `Include(Team)` | `AssociateSyncedTransitions` builds `new WorkItem(item, item.Team)`; the mocked unit tests never see the missing Team. The parity test must assert the loaded Team (added to D-5). |
| F2 | Reuse `FeaturesOfPortfolio` for F6 | Drops the per-Team start forecasts and tracking, so start dates in Feature answers change. Reuse rejected (D-3, D-4). |
| F3 | Category set as a parameter | `IN` instead of `OR`; unordered rows are serialised per day (E7), so the capture may differ byte for byte. Option b chosen (D-2). |
| F4 | A predicate that client-evaluated before and now throws | None exists: all columns are mapped (E8), and the Feature reads already executed before anything was chained (E2). The Postgres proof is still owed: the parity tests. |
| F5 | Implementations in `Services` implementing Flow-metrics ports | R4's list would grow (newcomer `WorkItemRepository`). Forbidden; D-3 a adds no edge. |
| F6 | Materialising early adds statements | Every call site enumerates once (E4); the query-count test pins W1. |
| F7 | Push the size chart's filter into SQL | Needs filtering and sorting to commute under the ordering comparer, which is unproven for source-order strings. The filter stays in memory. |
| F8 | Loose mocks hide a missed set-up | An unset read returns an empty list: the test goes red, not falsely green; the helper keeps the any-predicate semantics (D-6). |
| F9 | One read for both Done ∨ Doing and Doing ∨ Done | The OR terms swap at two call sites. The plan follows the `TeamId` filter and the rows are filtered after it, so the same order is expected; the parity test compares sequences for both spellings on both providers. Residue: shown, not proven, for SQLite plans on large data; the capture covers the real backup. |

A second set of stressors, walked through and not executed: Docker absent on a developer machine → the parity tests
cannot run in the fast loop; they are in the Integration tier and run in CI (residue: such a developer relies on CI).
The backup database missing in the examiner's environment → `metrics-answers-compare` cannot run → that EXAMINE
expectation is INDETERMINATE, not met (owner: the host). A refresh writing items between two reads in one request →
unchanged from today (no read is added).

## Wave: DESIGN / [REF] Cross-cutting

| Item | Answer |
|---|---|
| Behaviour-free | The same SQL per read, the same graph, the same order, the same statement count; pinned by D-5 and D-6. |
| RBAC | N/A, because no endpoint, guard or authorisation call changes. |
| Lighthouse-Clients (CLI, MCP) | N/A, because no route, DTO or enum changes. |
| Website | N/A, because nothing a user sees changes. |
| Usage-data event | N/A, because no feature becomes usable or is used differently. |
| UI sketch / loading and failure look | N/A, because no view is added or changed. |
| Terminology | No text a user sees; method names use the state-category words, not a tracker's words. |
| Payoff | No performance claim, so nothing to measure. |
| Program.cs | Not touched, so the shared-path full CI run is not forced. |
| Mutation | D-9. |

## Wave: DESIGN / [REF] SSOT edits pending (finalize checklist)

- `ARCHITECTURE.md` §6: the metrics services read other contexts' data through named reads on the owner repositories; `GetAllByPredicate` callers 13.
- ADR-270: nothing required (no edge, layout or ownership change); optionally a dated note under Decision 8 that a consumer's reads become named reads on the owner's contract until the owner is packaged.
- `brief.md`: a `## Application Architecture — story-6374 s05f metrics reads` section.
- Epic catalogue: Improvement log entry (KPI 4 15 → 13, KPI 12); S05g's AC list without the two services; S02's open question 4 points at D-8.
- No new ADR proposed; if one is wanted, its number comes from `origin/main` at write time.

## Wave: DESIGN / [REF] Open questions (for the maintainer)

1. D-3: named reads on the owner repositories (a) rather than Flow-metrics ports in the module (b)? This reads ADR-270's Decision 8 over Decision 6's "ports next to the use case", and changes the checklist item on R5.
2. D-10: accept a prefactoring move without its own RED, with the S05a list edit as each nwdes feature's semantic RED?
3. D-6: accept "Assert lines unchanged, Arrange lines retargeted" as "existing tests unmodified"?
4. D-11: confirm the formal-verification waiver (or allow `quint`, `java` and `agda` in the shell allowlist for a probe).
5. D-8: leave `UpdateTeamMetrics`' `Save` to S07b or S03b?

**Maintainer decision (DESIGN hold, 2026-11-10): all five recommendations accepted.** D-3 option a (reads on the
owner repositories; the checklist item on R5 is replaced accordingly), D-10 accepted, D-6 accepted, D-11 formal
verification waived (claims stay INDETERMINATE; the parity tests, the query-count test and the answer comparison carry
the evidence), D-8 left to S07b or S03b. Next: DEVOPS, then DELIVER (rigor profile agreed at its start).

## Wave: DESIGN / [REF] Architecture summary

A learning aid for the maintainer, not a replacement for `brief.md` or the ADRs.

**1. The shape in hexagonal terms (N new, M modified, U unchanged).**

- Driving adapters: the metrics controllers, the refresh-event policies, the hosted filler (U).
- Application: `ITeamMetricsService` / `IPortfolioMetricsService` (U); `TeamMetricsService` (M: 14 reads through named reads), `PortfolioMetricsService` (M: 13 reads, takes `IFeatureRepository`); `BaseMetricsService` (U).
- Domain (`Models`): `WorkItem`, `Feature`, the transitions (U); no new value object.
- Driven ports: `IWorkItemRepository`, `IFeatureRepository`, the two transition repositories (M: 13 new named reads); `GetAllByPredicate` (U, kept for S05g).
- Driven adapters: the four EF repositories (M); no new external call; Program.cs (U).

```text
 BEFORE                                          AFTER
 TeamMetricsService ──┐                          TeamMetricsService ──┐
 PortfolioMetrics   ──┤ GetAllByPredicate(expr)  PortfolioMetrics   ──┤ DoneItemsOfTeam(id),
 (Application)        │ then .Where/.Count       (Application, M)     │ TransitionsOfFeatures(ids), ...
                      v                                               v  IReadOnlyList<T> / int
 IRepository<T>  (IQueryable leaks out)          IWorkItemRepository / IFeatureRepository /
                      |                          transition repositories (M): same predicate,
                      v                          include and order, inside the owner
 EF repositories ── LighthouseAppContext         EF repositories (M) ── LighthouseAppContext (U)
```

**2. The patterns weighed.**

- Repository with intention-revealing reads: **chosen**; 27 calls collapse to 13 reads of the owner's data.
- Query object per capability (S12a): rejected; these are reads of an owner's data, not capabilities.
- Consumer-owned ports in the module with a module persistence adapter (dependency inversion, ADR-270 Decision 6): rejected; it reads another context's tables and copies how a Feature is read (Decision 8).
- Interface segregation (`IWorkItemReader`, like `IFeatureListReader`): deferred to S05g or S03b; it needs Program.cs and constructor changes.
- A value object or a parameter for the category set: rejected; it changes the SQL and may change a row order the answers show.
- Specification pattern (handing predicates in): rejected; that is the leak this Story removes.
- `AsNoTracking` or projections for speed: rejected; they change identity semantics and need a measured payoff (S13's territory).
- Unit of work for `UpdateTeamMetrics`: not here (D-8).
- Domain events, outbox, optimistic concurrency, strategy: not needed (no cross-context reaction, no write, no new variation).

## Wave: DEVOPS / [REF] Platform lens (2026-11-10)

Platform risk is low: S05f moves 27 reads into named methods on four existing repositories. It changes no route,
schema, configuration, environment or deployment. The lens is answered item by item, so nothing is skipped silently;
no platform-architect dispatch or operational review ran, because nothing below needs a design decision.

| Item | Answer |
|---|---|
| Environments and toolchain | Unchanged: .NET 10 SDK, the existing test project, no new package. The parity tests reuse the Testcontainers Postgres and SQLite set-up of `Integration/Containers/FeatureListParityOnPostgresTests`, so Docker is needed locally and in CI, as it already is. |
| Deployment and rollback | No migration, no `appsettings` key, no Docker or installer change; the EF model does not change (`ef-model-unchanged` EXAMINE command). Rollback is a plain revert of the commit range. |
| Observability | Unchanged: no type moves namespace, so log categories stay the same. The SQL each read sends is the same as today (same predicate per read, D-2); `RealityCheckQueryCountTest` pins the number of statements. |
| Recovery and failure modes | Unchanged: the reads keep their tracking, includes and in-memory ordering (D-4), and add no retry, timeout or new exception path. |
| CI cost | The touched files (`Services/Interfaces/Repositories`, `Services/Implementation/Repositories`, `Modules/FlowMetrics/Application`, tests) match `BACKEND_REGEX` and none matches `SHARED_REGEX`, so a push runs the backend selection without the live connector suites. No `Program.cs` edit. |
| Release-build rules | The S05a guard is a source scan and the R3–R5 rules read the IL; neither changes shape. The Architecture tests run once with `-c Release` at the end of each nwdes feature. |
| Test-time budgets and test lists | The two new parity classes are integration tier (container). They must stay under the backend per-class budget of 34 s (`Scripts/test-timings/test-time-budget.json`); no ledger entry is planned. `TestHostUseGuardTest.HostUsersStillToMove` is not touched: the parity tests start no web host. |
| Mutation (finalize) | Stryker.NET, whole-file `mutate` entries for the two services and the four repositories; the test filter names the metrics unit tests and the two parity classes. Bar: no file lower than its baseline (D-9); the new read methods at 80 % or more. Prove the scope from `reports/mutation-report.json`. |
| Usage-data event | N/A, because no feature becomes usable or used differently: the answers are byte-identical by design. Nothing is added to `UsageDataEventName` or `docs/settings/usagedata.md`. |
| Verification commands | `Lighthouse.Backend/Test-Fast.ps1` (fast loop); the slice's parity test class; `dotnet build` (zero warnings); the filtered `dotnet test` from CLAUDE.md (gate); `dotnet test -c Release --filter FullyQualifiedName~Architecture`. No frontend command: no frontend file changes. |

DEVOPS hands DELIVER no new constraint beyond Docker for the parity tests and their 34 s per-class budget.

## Wave: DELIVER / [REF] Maintainer decisions at DELIVER start (2026-11-10)

- **Rigor profile: `thorough` for all four slices** (A-P, A-S1, B-P, B-S1). Acceptance designer and crafter on `opus`; reviewers and EXAMINE on `sonnet`. `model` is passed on every dispatch.
- **UI sketch review: N/A**, because no slice changes anything a user sees (answers byte-identical by design).
- **Route:** two nwdes features on `main`, no lane: `story-6374-s05f-a-team-reads`, then `story-6374-s05f-b-portfolio-reads`.
