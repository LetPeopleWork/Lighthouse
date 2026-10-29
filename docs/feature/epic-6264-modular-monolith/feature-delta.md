<!-- markdownlint-disable MD024 -->
# Feature: epic-6264-modular-monolith

ADO Epic #6264, **Architecture Cleanup: Path to Modular Monolith**. Lighthouse is already a hexagonal modular monolith on paper (ARCHITECTURE.md §4, ADR-027) and partly in tests (ArchUnitNET, #5101). The code itself is still organised by technical layer. This Epic makes the modules, bounded contexts in DDD terms, visible in the folder tree on both stacks. It also enforces their boundaries by namespace and import rules, shapes ports by role instead of generic CRUD, and shrinks the shared kernel. It changes no behaviour, no topology and no part of the single-binary distribution.

ADO Epic: <https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/6264>

This Epic is a **story catalogue** rather than a slice plan. Its Stories are picked up one at a time over months, interleaved with other work. Each one runs its own DESIGN → DEVOPS → DISTILL → DELIVER, and each must be releasable on its own (D-6264-3).

## Wave: DISCUSS / [REF] Precursor inheritance

- **Primary source**: the Epic brief (ADO description, F1–F12, goal, non-goals, guardrails, DoD, risks, open questions). Its substance is carried below. Where today's code disagrees with a number in the brief, the brief is corrected in [REF] Outcome KPIs, and the real number is the baseline.
- **ADR-027** (`docs/product/architecture/adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md`) stands: D4 (single store, single binary, no broker), D5 (one assembly; re-examined only by S09b), D6 (CQRS-lite on one store), D7 (no Event Sourcing).
- **Epic #5121** delivered the dispatcher seam and the ArchUnitNET harness (#5101) that this Epic's ratchets build on.
- **ARCHITECTURE.md §4 lists nine modules**, including **Reports**, which was added after the brief was written. The brief says eight. This Epic adopts nine (D-6264-6).
- **DISCOVER and DIVERGE were explicitly skipped by the maintainer** (D-6264-1). There is no `diverge/` artefact. The risk is low because the Epic's direction is already fixed by ADR-027, and no user-facing choice is being made.

## Wave: DISCUSS / [REF] Persona ID

- **`lighthouse-maintainer`** (`docs/product/personas/lighthouse-maintainer.yaml`, alias `lighthouse-developer`): the core contributor who builds features and carries them through release. In this Epic they are the one who opens the repo, looks for "where does Forecasting live?", and either finds a folder or reads a table in ARCHITECTURE.md and grep-hunts. A second persona is the **CI build**, the one that says "no" when a boundary is crossed.
- No end-user persona. Every Story is `@infrastructure`.

## Wave: DISCUSS / [REF] JTBD (infrastructure-only framing)

Every Story is `job_id: infrastructure-only` and carries an `infrastructure_rationale` (D-6264-2). No entry is added to `docs/product/jobs.yaml`.

Shared rationale: the Epic is a behaviour-free refactor, and that is the whole point. Its value is observable by developers and CI only: a folder tree that names the nine contexts, ArchUnit and import-boundary rules that fail and name the offending file, ports that hand a consumer only what it needs, and shorter files. A user-visible change would be a defect against this Epic's guardrails. Each Story's own rationale names the specific developer- or CI-observable change.

## Wave: DISCUSS / [REF] DDD vocabulary used in this Epic

| DDD term | Meaning here | Where it lives in Lighthouse today |
|---|---|---|
| **Bounded context** | A part of the model with one consistent language and one owner. **One bounded context = one module.** | The nine modules of ARCHITECTURE.md §4. Today they are a table plus namespace or type-set ArchUnit rules, not folders. S01 confirms them, and S02/S03*/FE2 make them folders. |
| **Context map** | How contexts relate: upstream/downstream, customer/supplier, conformist, anti-corruption layer (ACL), shared kernel, published language. | Implicit in the §4 mermaid graph (legal cross-module edges only). S01 makes it explicit, with relationship types. |
| **Ubiquitous language** | The terms domain and code share, per context. | The seeded defaults in `TerminologySeeder.cs` (Feature, Work Item, Team, Portfolio, Delivery, Cycle Time, Throughput, WIP, Blocked, SLE). Code uses the seeded default terms, and Settings → Terminology renames only what a user sees. Drift: `Project` for Portfolio, the generic `*Service` suffix. S01 writes a glossary per context; FE6 and the backend renames follow it. |
| **Aggregate** | A consistency boundary: what must be valid within one transaction, changed only through its root. | The concurrency-tokened config roots (ARCHITECTURE.md §7): Team, Portfolio, WorkTrackingSystemConnection, Delivery, RBAC (UserProfile / RbacGroupMapping / ApiKey). Also Feature (the rich exception), WorkItem (anemic) and Report. S08 names them per context. **S13** then makes persistence follow those boundaries: today `Feature` loads drag in Forecasting-owned forecast rows (`Feature.Forecasts`/`StartForecasts` → `SimulationResults`), and the pilot makes Forecast its own aggregate referencing `FeatureId`. |
| **Value object** | An immutable, identity-less value, valid on construction. | Scattered in `Models` (e.g. `FeatureOrderKey`, `RemoteRecordStamp`, `StalenessPolicy`, `ReportOwner`, `UpdateKey`). `WorkTrackingSystemOptionsOwner` is a base class where a value object may fit (S07c). |
| **Domain event** | A fact that happened in one context, which another context reacts to. | `Models/Events/*` (15 events), published after commit via `IDomainEventDispatcher` (ARCHITECTURE.md §5). Transport only, not Event Sourcing (ADR-027 D7). S09a prefers events for cross-context reactions. |
| **Anti-corruption layer (ACL)** | A translating layer that keeps an upstream model out of ours. | **WorkTracking-Integration**: the Jira / ADO / Linear / CSV / ServiceNow connectors translate tracker issues into `WorkItem` / `Feature`. `WorkItemCategoryCrossing` and the `*WorkItemMapper`s are the translation. S06 sharpens its port by capability. |
| **Shared kernel** | Model that several contexts share by agreement, kept small. | `Models` (242 files today), which mixes the true kernel with module-owned DTOs. S04a/S04b reduce it. |
| **Published language / open host** | A stable, documented contract offered to many downstreams. | The public HTTP API consumed by the SPA and by Lighthouse-Clients (CLI/MCP). Its shapes are frozen by this Epic. |

## Wave: DISCUSS / [REF] Scope Assessment

**Oversized by every heuristic, deliberately (maintainer decision D-6264-3).** The catalogue holds 39 Stories (33 backend, 6 frontend), touches all nine contexts on both stacks, and spans months. It is not split into separate Epics. Every Story is independently releasable: finished alone, it leaves trunk greener and the structure measurably better, even if no other Story ever ships. That already satisfies the "each Epic ships on its own" rule at Story granularity, so a split would add ceremony without value.

**Elephant-carpaccio slicing is deferred to each Story's own DESIGN and roadmap** (D-6264-4). Sizes below are Story-level (S ≤ 3 days, M ≈ 1 week, L > 1 week, and L must be carpaccio-sliced at its DESIGN). They are not ≤ 1-day slices.

## Wave: DISCUSS / [REF] Journey

The developer's journey is in `docs/product/journeys/epic-6264-modular-monolith.yaml`.

- **Before**: "Where does Forecasting live?" → a table in ARCHITECTURE.md, then `grep` across `Services/Interfaces` (42 loose files), `Services/Implementation` (58 loose files) and `Models` (242 files). A new `GetAllByPredicate` call or a controller injecting `IRepository<Team>` passes review unnoticed. In the frontend, a `components/` file importing from `pages/` compiles fine.
- **After**: `Modules/Forecasting/` holds the context: its domain, application contract and adapters. `dotnet test --filter FullyQualifiedName~ArchitectureTests` fails and names the file when a boundary is crossed. `pnpm biome check ./src` (or the chosen tool) fails and names the import.
- Emotional arc: **wary** (every move is a merge-pain risk) → **steady** (each Story is small, green and ratcheted, and the tree gets clearer one context at a time) → **confident** (the structure explains itself, and the rules keep it that way).

## Wave: DISCUSS / [REF] Story Map

### Backbone (developer activities, left to right)

| Decide the shape | Guard what exists | Package the contexts | Narrow the ports | Thin the adapters & services | Strengthen the domain | Close the kernel & contracts | Frontend mirror |
|---|---|---|---|---|---|---|---|
| S01, S02 | S04a, S05a, S11 | S03a–h | S05b–g, S06 | S07a–d, S12a–c | S08, S13, S10 | S04b, S09a, S09b | FE1–FE6 |

### Pick-up groups (an order of preference, not a schedule)

- **G0. Independent and cheap; can go first**: S04a, S05a, S11, FE1. Each one adds a ratchet or fixes housekeeping, depends on nothing, and is useful even if the Epic stops there.
- **G1. Decide**: S01 (bounded contexts, context map and layout ADR) → S02 (Metrics pilot). **S02 is a gate**: its lessons-learned amend the ADR and decide whether and how S03* continue (risk: churn without payoff).
- **G2. Can run before or alongside packaging** (they narrow and thin, so packaging moves less): S12c (before any S03 moves that context's DTOs), S12a, S12b, S05b, S05c, S05d, S05e, S06, S07a–d, S08 (catalogue step), FE4, FE5, FE6.
- **G3. Packaging, one context per Story, after S02's gate**: S03a–h, FE2 (per module), FE3.
- **G4. End states**: S04b (after all S03*), S05g (after S05a–f and the remaining callers), S09a (after all S03*).
- **G4b. After S08's catalogue**: S13 (aggregate boundaries in persistence; the Forecast pilot can start as soon as S08 has named the Forecast aggregate, before or alongside S10).
- **G5. Deliberately last**: S10 (spike first), then S09b (an ADR decision once its preconditions hold; it may conclude "no").

### Dependency graph

`==>` is a hard dependency (cannot start before). `-->` is soft (better after, not blocking).

```text
G0  S04a   S05a   S11   FE1
                          \
G1  S01 ==> S02 (pilot gate) ======================+
     \\                                             \\
      ==> FE2 <== FE1                                ==> S03a..S03h (one per context)
                                                         ^    ^
    S12c -------- before that context's DTO move -------+    |
    S05c/d/e, S06, S07a-d, S12a/b --- soft (less to move) ---+

    S05a ==> S05b..S05f ==> S05g
    S02 -?-> S05f            (S02's DESIGN decides whether it absorbs S05f)
    S03a..h ==> S04b
    S03a..h ==> S09a ==> S09b (also needs: no cycles between contexts)
    S08 (catalogue) ==> S13 (aggregate boundaries in persistence; Forecast pilot)
    S08 (catalogue) ==> S10 (table ownership follows aggregate ownership)
    S13 --> S10   (before or alongside; S13 removes cross-context navigation S10 then forbids)
    S03c (Forecasting packaged) --> S13 (soft; the Forecast aggregate's home)
    S03a..h ==> S10 (spike first)
    S01 (glossary) --> FE6, backend renames in S06/S03*
    FE1 --> FE3, FE4, FE5;   FE4 --> FE2 per module (soft)
```

### Priority Rationale

1. **G0 first**: four ratchets that cost a day or two each, stop drift today, and are worth having even if nothing else ships. FE1 is the frontend's only enforcement baseline. Nothing exists there now.
2. **S01 before any move**: without the layout and the context map, every later Story would invent its own folder shape.
3. **S02 as the explicit go/no-go**: the brief's own risk mitigation. Metrics has the most `GetAllByPredicate` calls and no namespace of its own, so it tests the layout hardest.
4. **G2 before G3 where possible**: thinning and narrowing first means packaging moves smaller, cleaner code. S12c is a hard precondition for moving DTOs.
5. **End states and S10/S09b last**: they need the whole codebase ready (all contexts packaged, contracts in place, no cycles), and S10 carries the EF-migration risk.

## Wave: DISCUSS / [REF] How to pick up a Story

When a Story is picked up, possibly months from now:

1. **Read**: this feature-delta's [REF] sections (DDD vocabulary, Story Map, Cross-Cutting Impact Checklist, Outcome KPIs + guardrails, Wave Decisions, Risks), the Story's brief in `slices/`, and, once S01 has landed, the bounded-context and layout ADR with its context map and glossary. Re-check the brief's numbers against the code, because baselines drift.
2. **Run the waves for that Story only**: DESIGN → DEVOPS → DISTILL → DELIVER. DESIGN produces the carpaccio slices (≤ 1 day each) and the roadmap. Re-run the 9-item DoR on the Story itself at pick-up.
3. **Recurring per-Story checklist** (every Story, every PR):
   - [ ] **Ratchet in the same PR**: the ArchUnit or import-boundary rule that the Story makes pass is added or tightened in that PR. Baselines are inline file lists, not counts, so the failure names the file. A rule is never loosened.
   - [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels and topologies are unchanged. The existing API, E2E and response tests stay green unmodified, apart from tests that move along with the logic they cover.
   - [ ] **Mutation score in the touched area does not drop** (Stryker.NET / Stryker, CLAUDE.md minimum 80% for new code). Run it last, on frozen code.
   - [ ] **Moves separate from edits**: a pure move or rename commit (`refactor(<module>): move …`) and a separate edit commit, so review and `git log --follow` stay readable.
   - [ ] **ARCHITECTURE.md updated in the same change** (§4 module map, anchors and diagram; §6 for read-side ports; §12 for frontend). The ADRs are updated where a decision changes.
   - [ ] **One context at a time**: old and new layouts coexist until that context is fully migrated.
   - [ ] **Terminology**: new names follow S01's glossary and the seeded default terms.
   - [ ] **Cross-cutting re-check**: RBAC, Lighthouse-Clients, Website, usage-data event and UI sketch, answered for this Story with no silent N/A (default answers are in [REF] Cross-Cutting Impact Checklist).

## Wave: DISCUSS / [REF] Stories

Conventions for every Story: `@infrastructure` · `job_id: infrastructure-only` · behaviour-free. AC are developer- or CI-observable. Each brief is at `slices/slice-NN-<id>-<name>.md`.

### Backend track

#### S01 — Bounded contexts, context map and layout (ADR)

`@infrastructure` · job_id: infrastructure-only · **Size M** · Depends on: none · Brief: `slices/slice-01-s01-bounded-contexts-context-map-layout.md` · Natural DESIGN owner: **nw-ddd-architect**

- infrastructure_rationale: docs-only. It records the target shape every later Story moves towards. Without it, each Story would invent its own layout.
- Narrative: the maintainer sees nine modules in a table but no agreed folder shape, no relationship types between them, and drifting names. One ADR, covering both stacks, frames the nine modules as bounded contexts. It draws the context map, sets the layout (backend `Modules/<Context>/…`, frontend feature folders), the shared-kernel allowed content and the naming conventions, and gives a ubiquitous-language glossary per context.
- Elevator Pitch. **Before**: "Where should a new Forecasting policy go, and may Reports call Metrics?" Answer: read §4, guess. **After**: open the new ADR and see the nine contexts, a context map that shows WorkTracking-Integration as the ACL in front of Jira/ADO/Linear/CSV/ServiceNow and Reports as a conformist downstream of Metrics, the folder template, and a glossary row `Portfolio → a set of Features tracked together → Portfolio (code drift: Project)`. **Decision enabled**: S02 pilots exactly this layout. Every later Story cites the ADR rather than re-deciding.
- AC:
  - [ ] ADR (Status Accepted) confirms **nine** contexts and records the correction from the brief's eight (Reports).
  - [ ] Context map, both an ASCII or mermaid picture and a table: each edge is typed (customer/supplier, conformist, ACL, shared kernel, published language). External upstreams (trackers, OIDC IdP) and downstreams (Lighthouse-Clients, PostHog) are shown. WorkTracking-Integration is named the ACL.
  - [ ] Ubiquitous-language glossary per context (term → meaning → code name) that flags drift: Project vs Portfolio, `*Service` suffixes, configurable Terminology display terms vs the seeded-default code names.
  - [ ] Layout for both stacks, interfaces co-located with implementations, shared-kernel allowed content, naming conventions (use case / policy / calculator / projector / query instead of `*Service`).
  - [ ] Resolves the brief's open questions "Is Platform/Persistence a module or composition-root infrastructure?" and "`Domain/Application/Adapters` vs vertical slices per use case", with reasons. Records an answer to "order of F5/F6 vs F3", or leaves it to S02.
  - [ ] ARCHITECTURE.md §4 links the ADR and states nine bounded contexts.

#### S02 — Metrics pilot (the go/no-go gate)

`@infrastructure` · job_id: infrastructure-only · **Size L** · Depends on: **S01 (hard)**; S05a (soft) · Brief: `slices/slice-02-s02-metrics-pilot.md`

- infrastructure_rationale: Metrics is the context with no namespace of its own (a type-set rule today) and the heaviest `GetAllByPredicate` user. Packaging it proves or disproves the layout before the remaining eight contexts move.
- Elevator Pitch. **Before**: Metrics is "`*MetricsService`, `Percentile*`, `XmR*`, `Baseline*`" flat in `Services/Implementation`. **After**: `Modules/Metrics/` exists. `dotnet test --filter FullyQualifiedName~ArchitectureTests` runs a namespace-based Metrics rule in place of the type-set rule. The ADR carries a dated "lessons from the pilot" amendment. **Decision enabled**: continue with S03* as-is, continue with an amended ADR, or stop the packaging track.
- AC:
  - [ ] Metrics types live under the ADR's folder and namespace. The type-set ArchUnit rules are replaced by namespace rules in the same PR.
  - [ ] Metrics reads persistence through read ports, not `IRepository<T>`, or S02's DESIGN explicitly hands that to S05f.
  - [ ] All tests green, API responses unchanged, mutation score in Metrics not lower.
  - [ ] ADR amended with lessons learned and an explicit **go / go-with-changes / stop** for S03*.

#### S03a–S03h — Package each remaining context (one Story per context)

`@infrastructure` · job_id: infrastructure-only · **Size M–L each** · Depends on: **S01 (hard), S02 gate (hard)**; S12c for that context's DTOs (hard before moving them); S05c/d/e, S06, S07* (soft) · Briefs: `slices/slice-03…10-…`

| Story | Context | Brief |
|---|---|---|
| S03a | WorkTracking-Integration | `slice-03-s03a-package-worktracking-integration.md` |
| S03b | WorkItems / Sync | `slice-04-s03b-package-workitems-sync.md` |
| S03c | Forecasting | `slice-05-s03c-package-forecasting.md` |
| S03d | Portfolio / Delivery | `slice-06-s03d-package-portfolio-delivery.md` |
| S03e | RBAC / Identity | `slice-07-s03e-package-rbac-identity.md` |
| S03f | Platform / Persistence (or composition-root infrastructure, per S01) | `slice-08-s03f-package-platform-persistence.md` |
| S03g | Refinement | `slice-09-s03g-package-refinement.md` |
| S03h | Reports | `slice-10-s03h-package-reports.md` |

- infrastructure_rationale: each Story moves one context out of the layer-first folders into its own, so the tree names it. Behaviour is unchanged.
- Elevator Pitch (template). **Before**: the context is spread over `Services/Interfaces/<X>`, `Services/Implementation/<X>`, loose files and `Models`. **After**: `Modules/<Context>/` holds it. Its ArchUnit rule is namespace-based. The count of files left in `Services/Interfaces` + `Services/Implementation` drops by that context's files. **Decision enabled**: the next context can be packaged, and S04b/S09a get one step closer.
- AC (each):
  - [ ] No file of that context remains in `Services/Interfaces` or `Services/Implementation`. Interfaces are co-located with their implementations.
  - [ ] Context-owned DTOs move from `Models` into the context's Application contract, after S12c has made them pure mapping.
  - [ ] The context's ArchUnit rule is by namespace. Existing rules (e.g. "WorkTracking-Integration is a leaf input adapter", "Refinement depends down only") are preserved in the new namespaces.
  - [ ] Move commits separate from edit commits. ARCHITECTURE.md §4 row, anchors and diagram updated in the same change.
  - [ ] Behaviour-free, all tests green, mutation score held.

#### S04a — Close the `Models ↛ Services` gap

`@infrastructure` · job_id: infrastructure-only · **Size S** · Depends on: none (G0) · Brief: `slices/slice-11-s04a-close-models-services-gap.md`

- infrastructure_rationale: the kernel must not depend on the core. Two types (`IEntity`, `WorkTrackingSystems`) keep a documented gap open, and closing it turns a prose caveat into an enforced rule.
- Elevator Pitch. **Before**: ARCHITECTURE.md §4 carries "`Models ↛ Services` is a known, documented gap". **After**: `dotnet test --filter FullyQualifiedName~ArchitectureTests` runs `Models ↛ Services` and it is green, and the caveat is deleted from §4. **Decision enabled**: the kernel can now be shrunk safely (S04b).
- AC:
  - [ ] `IEntity` (today `Services/Interfaces/IEntity.cs`) and `WorkTrackingSystems` (today `Services/Implementation/WorkTrackingConnectors/WorkTrackingSystems.cs`) relocated into the kernel, behaviour unchanged.
  - [ ] ArchUnit rule `Models ↛ Services` added in the same PR and green. Any further violation the rule finds is fixed or baselined by inline file list in that PR.
  - [ ] ARCHITECTURE.md §4 gap note removed.

#### S04b — Reduce `Models` to the shared kernel + kernel-size rule

`@infrastructure` · job_id: infrastructure-only · **Size M** · Depends on: **all S03* (hard)**, S04a (hard) · Brief: `slices/slice-12-s04b-reduce-models-to-kernel.md`

- infrastructure_rationale: once every context owns its DTOs, `Models` should hold only what is truly shared (IDs, value objects, domain-event base types) and a rule should keep it that way.
- Elevator Pitch. **Before**: `Models` holds 242 files, entities and module DTOs alike. **After**: `Models` holds only the kernel types the S01 ADR allows. An ArchUnit rule fails naming any context-specific type added there. **Decision enabled**: S09a can define contracts on top of a small kernel.
- AC:
  - [ ] Every type left in `Models` is on the ADR's allowed-content list. The rest has moved to its owning context.
  - [ ] Kernel rule (e.g. "no type in `Models` is referenced by only one context", or an allow-list) added in the same PR.
  - [ ] Behaviour-free, tests green.

#### S05a — Ratchet guard: `GetAllByPredicate` callers and controllers on repositories

`@infrastructure` · job_id: infrastructure-only · **Size S** · Depends on: none (G0) · Brief: `slices/slice-13-s05a-ratchet-getallbypredicate-and-controller-repos.md`

- infrastructure_rationale: stops the generic-repository leak from growing today, and makes every later port Story's progress visible as a shrinking list.
- Elevator Pitch. **Before**: a new `workItemRepository.GetAllByPredicate(...)` in a service passes review. **After**: `dotnet test --filter FullyQualifiedName~ArchitectureTests` fails with "`Services/Implementation/Foo.cs` calls `GetAllByPredicate` outside the persistence adapters and is not on the baseline". When a listed file stops calling it, the test fails until the file is removed from the list. **Decision enabled**: S05b–g can each delete lines from a visible list.
- AC:
  - [ ] Given the inline baseline of **14** files (verified 2026-10-22; the brief said 12), when a file outside the persistence adapters that is not on the list calls `GetAllByPredicate`, then the guard fails naming that file.
  - [ ] Given a listed file no longer calls it, then the guard fails until the file is removed from the list (the list only shrinks).
  - [ ] Sibling rule in the same guard: controllers may not depend on `IRepository<>` / `I*Repository`. It is baselined by inline list (≈ **20** controllers verified 2026-10-22; the brief said 5; the DESIGN re-counts, including multi-line constructors) and works the same both ways.
  - [ ] Baseline 14: TeamMetricsService (14 calls), PortfolioMetricsService (13), ConnectionSaveService (5), WorkItemService (4), FeaturesController (3), WorkTrackingSystemConnectionUsage (3), OverTimeFillTarget (2), DemoPercentilesBackfillHandler (2), RefreshLogService (2), TeamMetricsController, BlockedCountSeriesBuilder, DemoBlockedHistoryBackfillHandler, RefinementList, WriteBackTriggerService.

#### S05b — Verify in-memory filtering through `Func` predicates; fix if confirmed

`@infrastructure` · job_id: infrastructure-only · **Size S–M** · Depends on: S05a (soft) · Brief: `slices/slice-14-s05b-verify-func-predicate-filtering.md`

- infrastructure_rationale: `GetByPredicate` / `Exists` take `Func<T,bool>`, which likely loads every row and filters in memory. If confirmed, this is the one Story with a possibly observable effect (performance), so results must stay byte-identical.
- Elevator Pitch. **Before**: nobody knows whether `teamRepository.Exists(t => t.Name == name)` loads all Teams. **After**: a test against SQLite (not EF InMemory, which translates nothing) shows the generated SQL has a `WHERE`, or, if the suspicion was wrong, a recorded "verified: translated" note closes the question. **Decision enabled**: whether S05c–e must change signatures urgently or only for ISP reasons.
- AC:
  - [ ] Evidence recorded (generated SQL or row counts on a provider-backed test) for `GetByPredicate` and `Exists`.
  - [ ] If confirmed: the signatures take `Expression<Func<…>>`, or callers move to intention-revealing methods. Results are identical (same rows, same order), proven by existing tests plus a provider-backed one.
  - [ ] If refuted: the brief's claim is closed in ARCHITECTURE.md §8 or the Story's own record, with no code change.

#### S05c / S05d / S05e — Role-specific persistence ports for Team, Portfolio, WorkTrackingSystemConnection

`@infrastructure` · job_id: infrastructure-only · **Size M each** · Depends on: S05a (hard: the ratchet shrinks); S01 (soft, for naming) · Briefs: `slice-15-s05c-team-ports.md`, `slice-16-s05d-portfolio-ports.md`, `slice-17-s05e-connection-ports.md`

- infrastructure_rationale: `IRepository<Team>` (~37 injections) and `IRepository<Portfolio>` (~35) hand every consumer full CRUD plus `IQueryable`/`Expression`. Intention-revealing readers and stores state what each consumer actually needs and keep EF semantics in the adapter.
- Elevator Pitch. **Before**: `ConnectionSaveService` calls `teamRepository.GetAllByPredicate(team => team.WorkTrackingSystemConnectionId == connectionId)`. **After**: it calls `teamReader.ForConnection(connectionId)`. The S05a baseline loses that file, and `grep -c "IRepository<Team>"` drops to 0 in production code. **Decision enabled**: the next aggregate's ports, and finally S05g.
- AC (each):
  - [ ] Production code injects no `IRepository<Team>` (resp. `<Portfolio>`, `<WorkTrackingSystemConnection>`). It injects role-specific readers and stores (names per the S01 glossary).
  - [ ] No `IQueryable`, `Expression` or `Func` in those port signatures.
  - [ ] Affected files are removed from both S05a baselines in the same PR.
  - [ ] Behaviour-free, tests green, mutation score held.

#### S05f — Move the two metrics services off `GetAllByPredicate`

`@infrastructure` · job_id: infrastructure-only · **Size M** · Depends on: S05a (hard); **may be absorbed by S02, and S02's DESIGN decides and records it** · Brief: `slices/slice-18-s05f-metrics-services-off-getallbypredicate.md`

- infrastructure_rationale: 27 of the 52 calls outside the persistence adapters are in `TeamMetricsService` / `PortfolioMetricsService`, including `.Any()` evaluated per Feature. EF InMemory in tests translates nothing, so a predicate SQLite or Postgres cannot translate still passes there.
- Elevator Pitch. **Before**: `PortfolioMetricsService` chains `.Where/.Any` on `featureRepository.GetAllByPredicate(...)` 13 times. **After**: it calls named read-port methods (e.g. `featuresOf(portfolio).closedBetween(from, to)`), and both files leave the S05a baseline. **Decision enabled**: S05g can protect `GetAllByPredicate`.
- AC:
  - [ ] Both metrics services leave the S05a baseline. The read ports are provider-translatable, proven on a provider-backed test.
  - [ ] Metrics API responses unchanged (existing tests plus response snapshots green).

#### S05g — End state: `GetAllByPredicate` protected; ADR-163 generalised

`@infrastructure` · job_id: infrastructure-only · **Size S** · Depends on: **S05a–S05f and the remaining baseline callers (hard)** · Brief: `slices/slice-19-s05g-getallbypredicate-protected.md`

- infrastructure_rationale: once the baseline list is empty, the compiler can enforce it, and the narrowed-port approach becomes the written rule.
- Elevator Pitch. **Before**: the S05a guard holds a short list. **After**: `GetAllByPredicate` is `protected` on `RepositoryBase`, the S05a guard's list is empty (or the guard is deleted as redundant), and ADR-163 reads Accepted with a generalising note (or a new ADR supersedes it). **Decision enabled**: new code can only use intention-revealing methods.
- AC:
  - [ ] Remaining callers (FeaturesController, TeamMetricsController, WorkItemService, OverTimeFillTarget, BlockedCountSeriesBuilder, RefreshLogService, WriteBackTriggerService, RefinementList, WorkTrackingSystemConnectionUsage, demo backfill handlers last) migrated, possibly as sub-slices.
  - [ ] `IRepository<T>.GetAllByPredicate` removed from the interface and `protected` on `RepositoryBase`. The build fails on any outside call.
  - [ ] ADR-163 → Accepted with a generalising note, or a new ADR.

#### S06 — Connector capability interfaces

`@infrastructure` · job_id: infrastructure-only · **Size L** · Depends on: S01 (soft) · Brief: `slices/slice-20-s06-connector-capability-interfaces.md`

- infrastructure_rationale: `IWorkTrackingConnector` (15 members) forces connectors to throw `NotSupportedException` for capabilities they lack, which is an ISP/LSP smell papered over by `Supports*` flags. Capability belongs in the type system, and this is the ACL's port.
- Elevator Pitch. **Before**: the CSV connector throws `NotSupportedException` from 6 members, Linear from 6, ServiceNow from 8. Callers check `Supports*` flags. **After**: `grep -rn "throw new NotSupportedException" …/WorkTrackingConnectors` finds no throw from a member of an interface the connector implements. Callers test `connector is IIncrementalSource`. Jira Cloud vs Data Center (ADR-139) is documented as the remaining runtime capability. **Decision enabled**: new connectors implement only what they can do.
- AC:
  - [ ] Capability interfaces (e.g. `IWorkItemSource`, `IIncrementalSource`, `IWriteBackTarget`, `IConnectionValidator`, final names per S01). Callers check capability by type.
  - [ ] No connector throws `NotSupportedException` for a member of an interface it implements. Baseline **20** member throws (CSV 6, Linear 6, ServiceNow 8, verified 2026-10-22; the brief said CSV 5).
  - [ ] Jira Cloud vs Data Center runtime capability (ADR-139) mapped onto the interfaces and documented.
  - [ ] Backend naming drift fixed in the same Story: `GetFeaturesForProject(Portfolio project)` gets the S01 glossary name (internal-only, no wire change).

#### S07a — Split `WorkItemService` along use cases

`@infrastructure` · job_id: infrastructure-only · **Size L** · Depends on: S05a (soft) · Brief: `slices/slice-21-s07a-split-workitemservice.md`

- infrastructure_rationale: `WorkItemService` takes 20+ dependencies and carries an S107 suppression. Splitting it along use cases (refresh, transition sync, remaining work, Feature sync) makes each part testable alone.
- Elevator Pitch. **Before**: one `WorkItemService` with an S107 pragma. **After**: `grep -c "disable S107" WorkItems/` = 0, there are N named use-case classes each under the threshold, and the ArchUnit rule "`WorkItemService` is the sole writer of transitions" is restated for its successor. **Decision enabled**: S03b packages smaller pieces.
- AC:
  - [ ] Split by use case, not by parameter bundling. No `[FromServices]`, no parameter objects.
  - [ ] Its S107 suppression removed. The seam rules that name it are preserved for the successor type(s).
  - [ ] Behaviour-free, mutation score held.

#### S07b — `BaseMetricsService` inheritance → composition

`@infrastructure` · job_id: infrastructure-only · **Size M** · Depends on: S02 (soft) · Brief: `slices/slice-22-s07b-metrics-composition.md`

- infrastructure_rationale: Team and Portfolio metrics share behaviour through a base class, so a change to the base silently changes both. Composition makes the shared parts named collaborators.
- Elevator Pitch. **Before**: `TeamMetricsService : BaseMetricsService`. **After**: no `BaseMetricsService`. Team and Portfolio metrics compose shared calculators and queries, and `TeamMetricsService`'s S107 suppression is gone. **Decision enabled**: the frontend mirror (FE4 `BaseMetricsService` split) follows the same shape.
- AC:
  - [ ] `BaseMetricsService` deleted, with shared behaviour in composed collaborators.
  - [ ] Metrics responses unchanged. Mutation score held.

#### S07c — Evaluate `WorkTrackingSystemOptionsOwner` → `WorkTrackingOptions` value object

`@infrastructure` · job_id: infrastructure-only · **Size S** (evaluation) / M (if adopted) · Depends on: S08 catalogue (soft) · Brief: `slices/slice-23-s07c-worktracking-options-value-object.md`

- infrastructure_rationale: Team and Portfolio inherit tracker options from a base class. A value object might model that better. **"No" is a valid outcome.**
- Elevator Pitch. **Before**: `Team : WorkTrackingSystemOptionsOwner`. **After**: either an ADR records "keep the base class, because …", or `Team` and `Portfolio` hold a `WorkTrackingOptions` value object with an unchanged EF mapping. **Decision enabled**: S03d/S05c–d know the aggregate shape.
- AC:
  - [ ] A recorded decision with reasons, including the EF mapping and migration cost.
  - [ ] If adopted: no schema change unless expand-only and done via `CreateMigration`, and behaviour-free.

#### S07d — Remove the remaining S107 suppressions on services

`@infrastructure` · job_id: infrastructure-only · **Size M** · Depends on: S07a, S07b (soft: they remove two) · Brief: `slices/slice-24-s07d-remove-s107-on-services.md`

- infrastructure_rationale: an S107 suppression marks a class doing too much. Removing it by splitting, never by bundling parameters, is the SRP payoff.
- Elevator Pitch. **Before**: 9 non-API types carry `#pragma warning disable S107`. **After**: `grep -rn "disable S107" Lighthouse.Backend/Lighthouse.Backend --include=*.cs` lists only API types (handled by S12). **Decision enabled**: Sonar's S107 can be held at zero.
- AC:
  - [ ] Verified list at start (2026-10-22): WorkItemService, TeamMetricsService, ForecastRunPlan, OAuthService, ConnectionHealthService, DemoDataService, WorkTrackingAuthStrategyFactory, BlockedCountSnapshotRecordingHandler, DemoBlockedHistoryBackfillHandler. (**StubOAuthProvider in the brief is wrong**: its pragma is S1075, not S107.)
  - [ ] Each suppression removed by splitting. No `[FromServices]`, no parameter objects.

#### S08 — Aggregates and invariants

`@infrastructure` · job_id: infrastructure-only · **Size M (catalogue step)**, follow-ups sized at its DESIGN · Depends on: S01 (soft) · Brief: `slices/slice-25-s08-aggregates-and-invariants.md`

- infrastructure_rationale: `Team` has 1 method and `WorkItem` has 0. Invariants live in services, so any caller can skip them. Naming the aggregate roots and their consistency boundaries per context, then moving the invariants that belong to an aggregate onto it, makes invalid states unrepresentable. Pure calculators and policies stay as they are (brief non-goal).
- Elevator Pitch. **Before**: "a Team's throughput history must be ≥ 1 day" is checked somewhere in a service, maybe. **After**: a catalogue in the S01 ADR (or its own) lists, per context, the aggregate roots, what must hold within one transaction, and each service-enforced invariant with its target. The first follow-ups move invariants into `Team`/`Portfolio` constructors and mutators, and a unit test on the entity proves the invalid state cannot be built. **Decision enabled**: S10 derives table ownership from the aggregate boundaries, and per-aggregate follow-up Stories are created from the catalogue.
- AC:
  - [ ] Catalogue: for each context, the aggregate roots, consistency boundary, and service-enforced invariants (file, rule, proposed home: aggregate / value object / stays a policy).
  - [ ] Cross-aggregate references go **by ID**. The catalogue lists today's object references that cross an aggregate boundary.
  - [ ] Per-aggregate follow-ups are decided at S08's DESIGN (new Stories as needed, each releasable alone).
  - [ ] Pure calculators and policies explicitly left alone.

#### S09a — Context contracts + contracts-only cross-context rule

`@infrastructure` · job_id: infrastructure-only · **Size M** · Depends on: **all S03* (hard)**, S04b (soft) · Brief: `slices/slice-26-s09a-context-contracts.md`

- infrastructure_rationale: once contexts are folders, each one exposes a `Contracts` namespace (facade, DTOs, events) and everything else is off-limits to other contexts, enforced by ArchUnit. Cross-context reactions prefer domain events.
- Elevator Pitch. **Before**: Refinement may call any public type in Forecasting. **After**: `dotnet test --filter FullyQualifiedName~ArchitectureTests` fails naming the offender when `Modules/Refinement/...` references `Modules/Forecasting/Domain/...` instead of `Modules/Forecasting/Contracts`. **Decision enabled**: S09b can weigh assemblies on a codebase with clean seams.
- AC:
  - [ ] Each context has a `Contracts` namespace. A cross-context ArchUnit rule allows only `Contracts` (with a ratchet baseline if needed, by inline file list).
  - [ ] Direct cross-context service calls that are reactions are listed. Those moved to domain events are done via the existing dispatcher (transport only, ADR-027 D7).
  - [ ] Payoff noted: CI test selection by module path (extending today's path-based connector selection) becomes possible. It is sound only as far as the rules are enforced. CI speed work itself is Story #6302.

#### S09b — Decision: one assembly per context?

`@infrastructure` · job_id: infrastructure-only · **Size S–M (an ADR; may conclude "no")** · Depends on: **S09a (hard), all S03* (hard), no cycles between contexts (hard)** · Brief: `slices/slice-27-s09b-assembly-per-context-decision.md`

- infrastructure_rationale: re-checks ADR-027 D5 once the codebase is ready. Deliberately deferred.
- Elevator Pitch. **Before**: D5 says one assembly because of the single-binary publish. **After**: an ADR records "assembly per context: yes / no / partial", weighing compiler-enforced `internal`, sound CI test selection, Stryker scoped per context, and module test assemblies against one `DbContext` referencing everything, the migration-DLL `HintPath` setup, and Sonar coverage gaps from skipped assemblies. **Decision enabled**: whether a physical split Story exists at all.
- AC:
  - [ ] ADR with every weigh-point from the brief, including the #6302 note (measured CI slowness comes from test bodies, so a split helps only by skipping unaffected tests).
  - [ ] If "yes": a follow-up Story outline that keeps the single binary and topologies unchanged.

#### S10 — Data ownership per context (logical only)

`@infrastructure` · job_id: infrastructure-only · **Size L, spike first** · Depends on: **all S03* (hard)**; **S08 (hard: table ownership follows aggregate ownership)** · Brief: `slices/slice-28-s10-data-ownership.md`

- infrastructure_rationale: one `DbContext` with 33 `DbSet`s and no recorded owner means any context can write any table. Ownership follows the aggregate boundaries from S08: the context that owns an aggregate owns its tables and is their only writer. **Logical ownership only, in three parts**: (a) document which context owns each `DbSet`/table; (b) enforce that only the owner writes it; (c) in code, cross-context references go by ID and never navigate into another context's entities. **Database foreign keys and cascading deletes stay** (D-6264-13). No database, schema, table prefix or file per module (decided 2026-10-14).
- Elevator Pitch. **Before**: `WorkItems` rows are written from several contexts, and code navigates from one context's entity into another's. **After**: an ownership table (`DbSet` → owning context → aggregate) is in ARCHITECTURE.md §8. An ArchUnit or repository-level rule fails naming any non-owner writer, and another fails naming code that navigates across a context boundary instead of holding an ID. The database's FKs and cascades are untouched. **Decision enabled**: contexts can evolve their models independently in code, while the single store keeps its free integrity checks.
- AC:
  - [ ] Spike first: entity configuration per context, with the two migration assemblies (`Lighthouse.Migrations.Sqlite` / `.Postgres`) and `HintPath` build order, before any move.
  - [ ] Ownership documented for all `DbSet`s and derived from S08's aggregates. Owner-only writes enforced.
  - [ ] In code, cross-context references go by ID. No navigation property is followed into another context's entities, enforced by a rule naming the offender.
  - [ ] Database FKs and cascading deletes unchanged (e.g. a Report still goes when its Team or Portfolio goes). No schema change is needed. Any EF mapping change that becomes unavoidable is expand-only via `CreateMigration`.
  - [ ] Single store, Standalone with zero external dependencies, unchanged.

#### S13 — Aggregate boundaries in persistence

`@infrastructure` · job_id: infrastructure-only · **Size L** · Depends on: **S08 (hard: decides the aggregates)**; S03c (soft); related to but separate from S10 (before or alongside) · Brief: `slices/slice-39-s13-aggregate-boundaries-in-persistence.md`

- infrastructure_rationale: S08 names the aggregate boundaries, and S13 makes persistence follow them. Verified 2026-10-22: forecasts already have their own tables (`ForecastBase` holds `WhenForecast` and `StartForecast` rows, and `IndividualSimulationResult` holds one row per outcome bucket). But they are modelled as child collections of the Feature aggregate: `Feature.Forecasts` / `Feature.StartForecasts` are cascade FKs configured in `Data/LighthouseAppContext.cs` (≈ l.241–281), and Feature carries forecast logic (`GetLikelhoodForDate`, `AggregatedWhenForecast`). So every Feature load drags in its whole forecast history: `FeatureRepository.cs:59–66` includes Portfolios, FeatureWork→Team, Forecasts→SimulationResults, StartForecasts→SimulationResults and DependsOnReferences, and `PortfolioRepository` and `DeliveryRepository` include Forecasts too. **This is the one Story in the Epic with a measurable performance payoff** (alongside a possible S05b effect).
- Elevator Pitch. **Before**: loading one Portfolio's Features loads every forecast row and simulation bucket behind them. **After**: Forecast is its own aggregate in the Forecasting context, referencing `FeatureId`. Feature no longer navigates to it, readers go through a forecast read port, and the S13 measurement shows rows loaded and query time for a Feature and a Portfolio load, before vs after, on the dev database restored from a production-like backup (`Restore-DbBackup.ps1`). **Decision enabled**: which further candidates (FeatureWork/Team, the Portfolio and Delivery includes, S08 findings) are worth the same treatment, judged by measured payoff.
- AC:
  - [ ] Catalogue of every aggregate whose load pulls in data owned by another bounded context: Forecasts on Feature (pilot), FeatureWork/Team, the Portfolio and Delivery includes, plus whatever S08's catalogue finds. Each with a before measurement.
  - [ ] Pilot: Forecast is an aggregate in the Forecasting context referencing `FeatureId`. `Feature` has no navigation to forecasts and no forecast logic. Readers use a forecast read port.
  - [ ] The table already exists, so the pilot is mostly a code change. Any schema change is expand-only via `CreateMigration`. **Database FKs and cascades stay** (D-6264-13): deleting a Feature still deletes its forecasts.
  - [ ] Forecast results, behaviour, API and DTO shapes unchanged (response snapshots, E2E).
  - [ ] Rows loaded and query time measured before and after for each candidate done, against the restored dev database. The KPI target is set at DESIGN.

#### S11 — `PublishDir` outside the project folder

`@infrastructure` · job_id: infrastructure-only · **Size S** · Depends on: none (G0) · Brief: `slices/slice-29-s11-publishdir-outside-project.md`

- infrastructure_rationale: publish output nests recursively inside the project (`Lighthouse.Backend/Lighthouse.Backend/publish/linux-x64/publish/...`, ~151 MB locally) and pollutes globbing, search and builds.
- Elevator Pitch. **Before**: `dotnet publish` writes inside the project folder. **After**: the publish output lands outside the project, and a second publish does not nest. **Decision enabled**: none needed. Housekeeping.
- AC:
  - [ ] `PublishDir` outside the project. CI, release workflows and Docker builds that read the old path updated in the same PR.
  - [ ] Published binary unchanged (size within noise, same files).

#### S12a — Metrics controllers → read-side query ports per capability

`@infrastructure` · job_id: infrastructure-only · **Size L** · Depends on: S07b (soft); S02 (soft) · Brief: `slices/slice-30-s12a-metrics-controllers-query-ports.md`

- infrastructure_rationale: `TeamMetricsController` (661 lines) and `PortfolioMetricsController` (675) hold view selection (`Get*ForView`), blackout-day annotation (`GetBlackoutDayIndicesArray`, `AnnotateBlackoutDays`) and `ReconcileReconstructedCountWithSnapshot`. That logic can only be tested through HTTP today.
- Elevator Pitch. **Before**: each behaviour case boots a `WebApplicationFactory` host (~1 s each). **After**: the logic lives in query ports per capability (following `IPercentilesOverTimeSeriesQuery` / `IProcessBehaviorSeriesQuery` / `IRefinementViewQuery`), and each behaviour is a plain unit test. Each endpoint keeps one HTTP test. Portfolio uses the shared ports, and its private copies are deleted. **Decision enabled**: S12b follows the same pattern.
- AC:
  - [ ] Team first, then Portfolio onto the same ports, with Portfolio's private copies deleted.
  - [ ] Recorded rule for what lives in query ports vs `I*MetricsService`, so there are never two read layers. ARCHITECTURE.md §6 describes the convention.
  - [ ] Both controllers' S107 suppressions removed.
  - [ ] Routes, DTO shapes and serialized responses unchanged. One HTTP test per endpoint kept, and per-behaviour cases moved to unit tests.

#### S12b — Other wide controllers: split by capability or move logic to a query port

`@infrastructure` · job_id: infrastructure-only · **Size M–L** (per-controller sub-slices at DESIGN) · Depends on: S12a (soft: pattern) · Brief: `slices/slice-31-s12b-split-wide-controllers.md`

- infrastructure_rationale: DeliveriesController, FeaturesController, PortfolioController and PortfoliosController suppress S107, so non-HTTP behaviour is reachable only through HTTP.
- Elevator Pitch. **Before**: 4 controllers carry S107 pragmas. **After**: sibling controllers per capability (same route prefix, same `RbacGuard`), or logic moved into query ports. `grep "disable S107" API/*Controller.cs` = 0. **Decision enabled**: S05a's controller-on-repository baseline shrinks too.
- AC:
  - [ ] No `[FromServices]` action injection, no parameter-object bundling.
  - [ ] Same routes, same `RbacGuard` on every action (RBAC ArchUnit rules preserved), same responses.

#### S12c — DTOs become pure mapping

`@infrastructure` · job_id: infrastructure-only · **Size M** · Depends on: none hard. **Must precede any S03* that moves that context's DTOs** · Brief: `slices/slice-32-s12c-dtos-pure-mapping.md`

- infrastructure_rationale: `FeatureDto` and `WorkItemDto` compute derived values (age, blackout-adjusted figures, blocked state) and suppress S107. Moving the derivation to the read side makes DTOs safe to relocate into context contracts.
- Elevator Pitch. **Before**: `new WorkItemDto(workItem, calendar, …)` computes age inside the DTO. **After**: a small read model or factory computes the values and the DTO only maps. Both S107 pragmas are gone. **Decision enabled**: S03* may move these DTOs.
- AC:
  - [ ] Derived values computed on the application or read side and passed in as plain values. DTOs hold data only.
  - [ ] Serialized JSON byte-identical (response snapshots), and the derivation is unit-tested without HTTP.

### Frontend track (`Lighthouse.Frontend/src`)

#### FE1 — Import-boundary enforcement + ratcheting allow-list

`@infrastructure` · job_id: infrastructure-only · **Size S–M** · Depends on: none (G0; should go first on the frontend) · Brief: `slices/slice-33-fe1-import-boundary-enforcement.md`

- infrastructure_rationale: no boundary tooling exists (no `biome.json`, no ESLint, verified 2026-10-22), so layer leaks compile silently. The baseline makes them visible and stops new ones.
- Elevator Pitch. **Before**: `components/Common/ModifyTeamSettings` importing `pages/Teams/Edit/ForecastSettingsComponent` compiles fine. **After**: `pnpm build` (Biome `noRestrictedImports` in `prebuild`) or a dependency-cruiser step fails with "components → pages: ModifyTeamSettings.tsx imports pages/Teams/…", unless the file is on the allow-list. Removing a leak forces the list to shrink. **Decision enabled**: FE2 can move features into folders inside an enforced boundary.
- AC:
  - [ ] Tool chosen at DESIGN: Biome `noRestrictedImports` in the existing `biome.json`, dependency-cruiser, or Fallow (a short read-only trial first; see the Story brief). It runs in `pnpm build` or `pnpm test` so CI enforces it.
  - [ ] Allow-list baseline of **16** violations (survey 2026-10-22, re-counted by the tool at DESIGN): components→pages 7, hooks→pages/components 5, services→hooks 2, models→pages 1, utils→services 1.
  - [ ] A new violation fails naming file and import. A fixed one fails until removed from the list.
  - [ ] Zero Biome errors and warnings stays true. The build is not slower beyond noise.

#### FE2 — Package by module (pilot: Delivery, then one Story per module)

`@infrastructure` · job_id: infrastructure-only · **Size L** (pilot); further modules split at FE2's DESIGN · Depends on: **S01 (hard: layout covers frontend), FE1 (hard)**; FE4 (soft) · Brief: `slices/slice-34-fe2-package-by-module.md`

- infrastructure_rationale: the frontend is layer-first. `components/Common` holds 308 of 375 component files and `pages/Common` holds 166. Delivery (~40 files spread over `models/Delivery*`, `services/Api/DeliveryService.ts`, `pages/Portfolios/Detail/Components/DeliveryGrid` (31), `components/Common/DeliveryRuleBuilder`, `utils/deliveries`) is the pilot.
- Elevator Pitch. **Before**: Delivery lives in five top-level folders. **After**: `src/modules/delivery/` (name per S01) holds it, and the FE1 rule enforces that other modules import only its public entry. **Decision enabled**: the next module is packaged the same way.
- AC:
  - [ ] Delivery files moved per the S01 layout, with move commits separate from edits.
  - [ ] FE1 rule extended to "only the module's public entry is importable from outside", in the same PR.
  - [ ] Pixel- and behaviour-preserving: Vitest, Playwright and `@screenshot` tests green and unchanged.
  - [ ] DESIGN lists the per-module follow-up Stories.

#### FE3 — Shrink `src/models`

`@infrastructure` · job_id: infrastructure-only · **Size M–L** · Depends on: FE2 per module (soft), FE1 (hard) · Brief: `slices/slice-35-fe3-shrink-models.md`

- infrastructure_rationale: `src/models` (82 production files, verified; 317 importing files per survey) mixes zod wire schemas, view-logic classes, request/response types and UI row types, and even an `IDemoDataService`. It is the frontend's catch-all kernel.
- Elevator Pitch. **Before**: `models/Metrics/InfoWidgetData.ts` imports from `pages/`. **After**: module-owned API types live in their module. `src/models` holds only the small shared core (WorkItem, Feature, EntityReference, IFeatureOwner, confirmed at DESIGN), and an FE1 rule keeps it that way. **Decision enabled**: FE4's per-module ports have a home for their types.
- AC:
  - [ ] Shared-core list confirmed at DESIGN. Everything else moved to its owning module.
  - [ ] `IDemoDataService` out of `models`. Zod schemas stay at the trust boundary (§12, schema-first).
  - [ ] FE1 rule for `models` in the same PR. Behaviour-free.

#### FE4 — Per-module API ports

`@infrastructure` · job_id: infrastructure-only · **Size L** (split per module at DESIGN) · Depends on: FE1 (soft); mirrors S05 and S07b · Brief: `slices/slice-36-fe4-per-module-api-ports.md`

- infrastructure_rationale: `IApiServiceContext` has 33 members and 75 production consumers via `useContext` (verified 2026-10-22). The test double `MockApiServiceProvider.ts` (518 lines, 196 `vi.fn`) is imported by 133 test files. Narrow per-module ports state what each consumer needs, the same way S05 does on the backend.
- Elevator Pitch. **Before**: `useContext(ApiServiceContext)` gives a component all 33 services. **After**: `grep -rn "useContext(ApiServiceContext)" src --include=*.tsx --include=*.ts | grep -v test` count falls per module towards 0. `deserializeTeam/Portfolio/Features` have left `BaseApiService`. The generic `IMetricsService<T>` (~33 methods, `MetricsService.ts` 964 lines) and its `BaseMetricsService` inheritance are split. **Decision enabled**: FE2 modules own their ports.
- AC:
  - [ ] Per-module port hooks or contexts. Test doubles per port, so a test mocks only what it uses.
  - [ ] Metrics service split. Composition instead of inheritance, mirroring S07b.
  - [ ] The print route's `Pick`-based ports (§12) keep their runtime key tests.
  - [ ] Behaviour-free. No TanStack Query migration (out of scope).

#### FE5 — Split wide units

`@infrastructure` · job_id: infrastructure-only · **Size L** (split per unit at DESIGN) · Depends on: FE1 (soft), FE4 (soft) · Brief: `slices/slice-37-fe5-split-wide-units.md`

- infrastructure_rationale: very large files resist review and change. `BaseMetricsView.tsx` (2034 lines, 87 imports), `hooks/useMetricsData.ts` (970), `ModifyConnectionSettings.tsx` (1201), `DeliverySection` (993), `DeliveryCreateModal` (914). The RBAC member and group-mapping handlers are duplicated in `TeamDetail.tsx` (≈ l.144–211) and `PortfolioDetail.tsx` (≈ l.114–185).
- Elevator Pitch. **Before**: `BaseMetricsView.tsx` has 2034 lines. **After**: the count of production files over 1000 lines (`find src -name '*.ts*' ! -name '*.test.*' | xargs wc -l | awk '$1>1000'`) is 0, or each remaining file carries a recorded justification. The duplicated RBAC handlers are one shared hook that still goes through `useRbac` / `RbacService`. **Decision enabled**: FE2 moves smaller units.
- AC:
  - [ ] Each unit split by responsibility, pixel- and behaviour-preserving (`@screenshot` and Playwright unchanged).
  - [ ] Shared RBAC-handler hook. All RBAC gating still derives from `useRbac()`, and no component fetches `/api/latest/authorization/my-summary` directly.
  - [ ] DESIGN counts the >1000-line baseline exactly.

#### FE6 — "Project" → Portfolio naming in the frontend

`@infrastructure` · job_id: infrastructure-only · **Size M** · Depends on: **S01 glossary (hard: the source of truth for names)** · Brief: `slices/slice-38-fe6-project-to-portfolio-naming.md`

- infrastructure_rationale: about 21 production and 17 test files still call a Portfolio "Project" (`ModifyProjectSettings`, `ProjectMetricsService`/`IProjectMetricsService`, `projectSettings`, `getProjectSettings…`, `projectId` in `UpdateSubscriptionService`/`MetricsService`). The ubiquitous language in code drifts from the domain.
- Elevator Pitch. **Before**: `ModifyProjectSettings` edits a Portfolio. **After**: the code names match the S01 glossary, and `grep -rnwi "project" src --include=*.ts* | grep -v test` lists only the documented exceptions. **Decision enabled**: new code reads in one language.
- AC:
  - [ ] Renames follow the S01 glossary. User-visible copy unchanged (it comes from Terminology anyway).
  - [ ] **Out of FE6**: the wire field `Feature.projects`. It is an API contract read by Lighthouse-Clients CLI/MCP and needs a versioned-API decision, recorded as out of scope. Also not drift: Jira `projectKey`, `getStatesForProjects`, `/suggestions/*/projects` (tracker-native "project").
  - [ ] Backend counterpart (`GetFeaturesForProject`) is handled in S06 (connector port) or in the owning context's S03 Story, not here.

## Wave: DISCUSS / [REF] Out of scope

- **CI test speed notes from the brief** (template DB per scenario, one Postgres container per run, Vitest `isolate: false`, frontend change-based selection): **Story #6302**. The one exception kept: module-based CI test selection is noted as a payoff under S09a/S09b.
- **TanStack Query migration** of frontend data fetching (47+ `useEffect` fetchers vs 14 `useQuery`, 0 `useMutation`). It is a behaviour change (caching, refetch timing), so it is its own future Epic. Candidate item for that Epic: `useRbac` re-fetches my-summary per mount.
- **Frontend test layout**: already co-located and healthy.
- **Team/Portfolio metrics view duplication** in the frontend: already generic via `BaseMetricsView`. Splitting that file is FE5.
- **`Feature.projects` wire rename**: needs a versioned-API decision with Lighthouse-Clients. Not this Epic.
- **Removing or replacing cross-module foreign keys or joins in the database.** A single store makes FKs free integrity checks, and some contexts rely on cascading deletes: Reports, for example, go when their owner goes. Removing FKs only pays off with physically separate stores, which are a non-goal (no database, schema, table prefix or file per module; decided 2026-10-14). S10 enforces ID-only references in code, not in the schema.
- From the brief's non-goals: no microservices, no extra deployables, no broker, no physical database or schema separation, no big-bang restructure, no forcing calculation logic onto entities.

## Wave: DISCUSS / [REF] Cross-Cutting Impact Checklist

Defaults for every Story. Each Story re-confirms at pick-up (step 3 of How to pick up a Story).

### RBAC

**No change to authorization behaviour.** The RBAC/Identity context is moved in **S03e**. `IRbacAdministrationService` stays the single entry for all RBAC business logic (ARCHITECTURE.md, CLAUDE.md). `RbacGuard` is unchanged on every action, including controllers split in S12b, which keep the same guard. `useRbac()` stays the only source of UI gating, and FE5's shared RBAC-handler hook goes through it, with no direct `/api/latest/authorization/my-summary` fetch. The existing RBAC ArchUnit and enforcement rules are preserved and re-pointed to the new namespaces in the same PR as each move.

### Lighthouse-Clients (CLI + MCP)

**N/A for every Story, because no route, DTO shape or serialized response changes** (S12* and S03* DTO moves are byte-identical by AC). No client release, no `FEATURE_REQUIRES_SERVER_NEWER_THAN` bump. **Explicitly out**: renaming the wire field `Feature.projects`, which clients read. That needs its own versioned-API decision.

### Website (marketing surface)

**N/A, because nothing user-visible changes.** A behaviour-free internal refactor has no marketable surface and no screenshot drift.

### Usage-data event (for DEVOPS)

**Provisional: N/A, because no Story adds a user-facing feature** whose use could be counted. Each Story's DEVOPS wave confirms.

### UI sketches

**N/A.** No Story changes anything a user sees, and every frontend Story is behaviour- and pixel-preserving (`@screenshot` tests unchanged). If a Story's DESIGN finds a visible change, it must sketch it and ask the maintainer then.

### Terminology

All docs in this Epic use the seeded default terms (Feature, Work Item, Team, Portfolio, Delivery, Cycle Time, Throughput, WIP, Blocked, SLE). S01's glossary makes the code-name vs display-term relation explicit.

## Wave: DISCUSS / [REF] Outcome KPIs + guardrails

### Objective

Anyone opening the repo can see the nine bounded contexts, what each one owns and what it exposes. Boundaries are enforced by namespace and import rules, ports are shaped by role, and the shared kernel is small, on both stacks, with no change in behaviour.

### KPI table (baselines verified 2026-10-22 unless marked "survey")

| # | Measure | Baseline (brief → verified) | Target | Measured by | Story |
|---|---|---|---|---|---|
| 1 | Files under `Services/Interfaces` + `Services/Implementation` | loose: brief 41 + 56 → **42 + 58**; total **155 + 352 = 507** | **0** | `find Lighthouse.Backend/Lighthouse.Backend/Services/{Interfaces,Implementation} -name '*.cs' \| wc -l` | S02, S03a–h |
| 2 | Contexts packaged under their own folder with namespace rules | **0 of 9** (Metrics is a type-set) | **9 of 9** (or 8 + Platform as composition root, per S01) | ArchUnit rule count per context, folder tree | S02, S03* |
| 3 | `Models` file count | brief ~218 → **242** (2026-10-22) → **246** (2026-10-27); S04a will add 6 kernel files, S04b reduces | only S01's kernel allow-list | `find …/Models -name '*.cs' \| wc -l` + kernel rule | S04b |
| 4 | Files calling `GetAllByPredicate` outside persistence adapters | brief 12 → **14** (52 calls; 2026-10-22 counted a comment) → **15** at landing (Epic 6350 added `ReportOwnerLookup`) | **0**, then `protected` | S05a guard list length | S05a–g |
| 5 | `API` types depending on `IRepository<>` / `I*Repository` | brief 5 → ≈20 → **23** (20 controllers + 3 API helpers, verified 2026-10-27) | **0** | S05a sibling-rule list length | S05c–e, S12* |
| 6 | Production `S107` suppressions | brief 18 → **17** (8 API + 9 others; StubOAuthProvider is S1075) | **0** | `grep -rln "disable S107" --include=*.cs Lighthouse.Backend/Lighthouse.Backend` | S07*, S12* |
| 7 | Connector `NotSupportedException` from members of implemented interfaces | brief 5 (CSV) → **20** (CSV 6, Linear 6, ServiceNow 8) | **0** | `grep -rn "throw new NotSupportedException" …/WorkTrackingConnectors/{Csv,Linear,ServiceNow}` | S06 |
| 8 | Frontend import-boundary violations | **16** (survey) → **21 imports in 16 files** (Fallow, 2026-10-29; FE1 lists imports, not files) | **0** | FE1 allow-list length | FE1, FE2, FE3 |
| 9 | `useContext(ApiServiceContext)` production consumers | **75** files | **0** (per-module ports) | `grep -rln "useContext(ApiServiceContext)" src \| grep -v test \| wc -l` | FE4 |
| 10 | Production files where "Project" means Portfolio | **≈21** (survey) | **0** (documented exceptions excluded) | grep per FE6 AC | FE6 |
| 11 | Frontend production files > 1000 lines | ≥ 2 known (2034, 1201), exact count at FE5 DESIGN | **0** or each justified | `wc -l` sweep | FE5 |
| 13 | Rows loaded and query time for a Feature load and a Portfolio load (restored production-like dev DB) | measured at S13's DESIGN (today every Feature load includes Forecasts→SimulationResults and StartForecasts→SimulationResults) | set at S13's DESIGN (pilot: no forecast rows loaded by a Feature load) | before/after measurement on `Restore-DbBackup.ps1` data (EF query logging + timing) | S13 |
| 12 | Mutation score on touched code | per area at pick-up | **not lower**; ≥ 80% on new code | Stryker.NET / Stryker | all |

### Guardrails (must NOT degrade)

- All tests green (backend NUnit filter per CLAUDE.md, Vitest, Playwright E2E twice in CI).
- Mutation score per touched area.
- API responses unchanged (response snapshots and E2E unchanged). Pixels unchanged (`@screenshot`).
- Single binary and the deployment topologies (Standalone, Docker, Helm/cluster) unchanged. ADR-027 D4.
- CI wall time not worse beyond noise.
- Published binary size within noise.
- Zero new SonarCloud issues, `TreatWarningsAsErrors` clean, Biome zero warnings.

## Wave: DISCUSS / [REF] Definition of Done (Epic)

From the brief, extended for the frontend:

- Every context has its own namespace and folder. No code is left in `Services/Interfaces` or `Services/Implementation`.
- ArchUnit enforces context boundaries by namespace, including the contracts-only rule between contexts.
- `Models` contains only shared-kernel types, and `Models ↛ Services` is green.
- No production code depends on generic `IRepository<T>`. Nothing outside the persistence adapters calls `GetAllByPredicate`, and no controller depends on a repository.
- No S107 suppression remains in production code (controllers, DTOs, services).
- No connector throws `NotSupportedException` for a member of an interface it implements.
- Aggregate roots and their invariants are catalogued per context. Persistence follows the aggregate boundaries: no aggregate load pulls in another context's data (S13), with the payoff measured. Table ownership per context is documented, derived from them, and enforced.
- The bounded-context and layout ADR, context map and glossary exist, and ARCHITECTURE.md and the ADRs reflect the new structure.
- **Frontend**: import boundaries enforced with an empty allow-list. Modules packaged per the ADR. `src/models` is the small shared core. No `useContext(ApiServiceContext)` outside the provider. No "Project" meaning Portfolio in code (outside documented exceptions). No unjustified file > 1000 lines.
- Behaviour, topologies and single-binary distribution are unchanged. All tests green, mutation scores hold.
- The S09b ADR is recorded (whatever its answer).

## Wave: DISCUSS / [REF] DoR Validation

Validated at **catalogue level** (D-6264-3). Every Story's DoR is **re-checked when it is picked up**, against the code as it is then, because baselines drift over months.

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement, domain language | PASS | Every Story has an `infrastructure_rationale` naming the concrete smell (file, count, rule). |
| 2 Persona with specifics | PASS | `lighthouse-maintainer` (existing persona) plus the CI build. All Stories `@infrastructure`. |
| 3 ≥ 3 domain examples, real data | PASS (catalogue) | Real file and type names and verified counts in AC and briefs (e.g. the 14 `GetAllByPredicate` files, the 20 connector throws, the 16 frontend violations). The per-Story DESIGN adds slice-level examples. |
| 4 UAT Given/When/Then (3–7) | PASS (catalogue) | 2–6 testable AC per Story, CI-observable. Ratchet Stories phrased Given/When/Then. Full G/W/T scenarios are written at each Story's DISTILL. |
| 5 AC derived from UAT | PASS | AC map 1:1 to the Elevator Pitch "After". |
| 6 Right-sized | PASS **by waiver** | Stories sized S/M/L, and carpaccio into ≤ 1-day slices happens at each Story's DESIGN (D-6264-4). L Stories flagged. |
| 7 Technical notes + cross-cutting | PASS | Cross-Cutting Impact Checklist (RBAC, Clients, Website, usage data, sketches, Terminology) answered explicitly. Constraints in Wave Decisions. |
| 8 Dependencies tracked | PASS | Hard and soft dependencies per Story plus the ASCII graph. |
| 9 Outcome KPIs, measurable | PASS | KPI table with baselines, targets and commands. |

**DoR status: PASSED at catalogue level. Per-Story re-check at pick-up is mandatory.**

Elevator-Pitch gate: all 39 Stories carry Before / After / Decision-enabled. "After" is concrete CI or developer output (`dotnet test --filter …ArchitectureTests` naming a file, `grep` counts, folder tree, `pnpm build` failure). **Slice-composition gate: knowingly waived** (D-6264-5): every Story is `@infrastructure` by design.

## Wave: DISCUSS / [REF] Wave Decisions

### Key Decisions

- **D-6264-1**: DISCOVER and DIVERGE explicitly skipped by the maintainer. Direction comes from ADR-027 and the Epic brief.
- **D-6264-2**: **infrastructure-only framing.** Every Story `job_id: infrastructure-only` + `infrastructure_rationale`. No `jobs.yaml` entry.
- **D-6264-3**: **Story catalogue, not a slice plan.** DESIGN and later waves run per Story, picked up one at a time over months and interleaved with other work. Each Story is independently releasable. One brief per Story.
- **D-6264-4**: **Carpaccio deferred** to each Story's own DESIGN and roadmap. DoR item 6 is satisfied by S/M/L sizing plus that deferral.
- **D-6264-5**: **Slice-composition gate knowingly waived** for this Epic by maintainer decision. A behaviour-free refactor has no user-visible slice by definition. That is the point, not a gap.
- **D-6264-6**: **Nine modules, not eight.** Reports (ARCHITECTURE.md §4 row 9) postdates the brief. S01 confirms the map.
- **D-6264-7**: CI test-speed notes out → Story #6302. Module-based CI test selection kept as a noted payoff of S09a/S09b.
- **D-6264-8**: Frontend track **in** (FE1–FE6), mirroring the backend. TanStack Query migration **out** (behaviour change, own Epic).
- **D-6264-9**: **DDD made explicit** (maintainer amendment). S01 becomes "Bounded contexts, context map and layout" (context map with relationship types, WorkTracking-Integration as the ACL, glossary per context; DESIGN owner nw-ddd-architect). S08 becomes "Aggregates and invariants". S10 derives table ownership from S08's aggregates. FE6 takes its names from S01's glossary. A [REF] DDD vocabulary section was added.
- **D-6264-10**: Backend `GetFeaturesForProject` rename goes into **S06** (it is a connector-port member reshaped there), not FE6.
- **D-6264-11**: **S05f stays a catalogue entry**, but S02's DESIGN may absorb it and must record whether it did.
- **D-6264-12**: Brief numbers corrected against code (2026-10-22): see KPI table (loose files 42/58, `Models` 242, `GetAllByPredicate` 14 files, controllers on repositories ≈ 20, S107 17, connector throws 20). Ratchet baselines use the verified lists.
- **D-6264-13**: **Database per module rejected; data ownership is logical only** (maintainer amendment 2). The reasons:
  - Cross-module transactions: sync writes Work Items and snapshots together.
  - The single-file Standalone store.
  - Backup and restore of one store.
  - No independent scaling need inside a single binary.

  S10 therefore (a) documents ownership per `DbSet`/table, (b) enforces owner-only writes, and (c) keeps cross-module references by ID in code, with no navigation into another module's entities. **Database FKs and cascading deletes stay.** Table ownership follows aggregate ownership (S08).

- **D-6264-14**: **New Story S13, "Aggregate boundaries in persistence"** (maintainer amendment 3). It depends on S08, is related to S10 but separate from it, and sits before or alongside S10. Pilot: Forecast becomes its own aggregate in the Forecasting context, referencing `FeatureId`, and Feature stops navigating to it. Each candidate is measured before and after on the restored production-like dev database. KPI 13 added; baseline and target are set at S13's DESIGN. FKs and cascades stay, and any schema change is expand-only.
- **D-6264-15**: **Event Storming drives the DDD Stories, run together with the maintainer.** S01's DESIGN opens with a Big Picture session: a draft board is seeded from the code (domain events, state transitions, commands, policies, read models, external systems), walked section by section in chat, and published as a visual board. The context map and glossary come out of that session rather than being assumed from the nine modules. S08's DESIGN opens with a design-level session per context, building on that board, to find the aggregate boundaries that S13 then applies to persistence.

### Constraints (inherited / cross-cutting)

- ADR-027 D4 (single store, single binary, no broker) and D7 (no Event Sourcing) stand. D5 is re-examined only by S09b.
- Ownership is logical only. No database, schema, prefix or file per module (decided 2026-10-14). Database FKs and cascades stay (D-6264-13).
- Ratchet only: rules are tightened in the same PR that makes them pass, never loosened. Baselines are inline file lists.
- EF migrations only via `CreateMigration`, expand-only per release. Migration DLLs are `HintPath` references (build first).
- RBAC via `IRbacAdministrationService`. UI gating via `useRbac()`.
- Comments: no internal references as explanations (CLAUDE.md).

### Upstream changes

- `docs/product/journeys/epic-6264-modular-monolith.yaml`: NEW (developer journey).
- No `jobs.yaml` change. No new persona.

## Wave: DISCUSS / [REF] Risks / Open questions for per-Story DESIGN

1. **Churn without payoff** (HIGH). Mitigation: the S02 pilot is an explicit go / go-with-changes / stop gate. G0 ratchets pay off even if the gate says stop.
2. **Merge pain from large moves** (MEDIUM, months of interleaved work). Mitigation: one context per Story, moves separate from edits, short-lived branches (trunk-based). Never autonomously rebase.
3. **EF Core migrations and the two migration assemblies** (S10, S03f) (HIGH). Mitigation: S10 last, spike first.
4. **Baseline drift over months** (MEDIUM). The verified numbers here will move. Every Story re-counts at pick-up, and the ratchets make drift visible rather than silent.
5. **Stryker staleness on moved files** (LOW–MEDIUM). Line-range mutate configs break on moves. Run mutation last, on frozen code.
6. **S05b and S13 change runtime cost** (MEDIUM). S13 is the Story with a deliberate, measured performance payoff, and S05b may have one. Results must stay identical in both: forecast results unchanged, API shapes unchanged. S13 also carries a lazy-load risk: a caller that relied on `Feature.Forecasts` being included must move to the read port, or it silently sees an empty list. Its DESIGN finds every reader before removing the navigation.
7. Open for S01: Platform/Persistence a module or composition-root infrastructure? `Domain/Application/Adapters` vs vertical slices? Frontend folder naming (`modules/` vs `features/`)?
8. Open for S01/S02: order of S05/S06 vs S03 (before, during or after packaging each context). The default here is G2 before G3 where possible.
9. Open for S09b: assembly per context. Deliberately deferred, may be "no".
10. Open for FE1 DESIGN: Biome `noRestrictedImports` (no new dependency, but path-pattern expressiveness is limited) vs dependency-cruiser (expressive, but a new dev dependency, which must respect pnpm's minimum release age).
11. `Feature.projects` wire rename needs a versioned-API decision with Lighthouse-Clients. Flagged, not owned here.

## Wave: DISCUSS / [REF] ADO Stories

Created 2026-10-22 as children of Epic #6264, all in state New. The Story ID in the title matches the catalogue above and the brief in `slices/`.

| ADO | Story |
|---|---|
| #6357 | S01 Bounded contexts, context map and layout (ADR) |
| #6358 | S02 Metrics pilot (go/no-go gate) |
| #6359 | S03a Package WorkTracking-Integration |
| #6360 | S03b Package WorkItems / Sync |
| #6361 | S03c Package Forecasting |
| #6362 | S03d Package Portfolio / Delivery |
| #6363 | S03e Package RBAC / Identity |
| #6364 | S03f Package Platform / Persistence |
| #6365 | S03g Package Refinement |
| #6366 | S03h Package Reports |
| #6367 | S04a Close the `Models ↛ Services` gap |
| #6368 | S04b Reduce `Models` to the shared kernel + kernel-size rule |
| #6369 | S05a Ratchet guard: `GetAllByPredicate` callers + controllers on repositories |
| #6370 | S05b Verify in-memory filtering via `Func` predicates; fix if confirmed |
| #6371 | S05c Role-specific persistence ports for Team |
| #6372 | S05d Role-specific persistence ports for Portfolio |
| #6373 | S05e Role-specific persistence ports for WorkTrackingSystemConnection |
| #6374 | S05f Move the two metrics services off `GetAllByPredicate` |
| #6375 | S05g End state: `GetAllByPredicate` protected; ADR-163 generalised |
| #6376 | S06 Connector capability interfaces |
| #6377 | S07a Split `WorkItemService` along use cases |
| #6378 | S07b `BaseMetricsService` inheritance → composition |
| #6379 | S07c Evaluate `WorkTrackingSystemOptionsOwner` → `WorkTrackingOptions` value object |
| #6380 | S07d Remove the remaining S107 suppressions on services |
| #6381 | S08 Aggregates and invariants |
| #6382 | S09a Context contracts + contracts-only cross-context rule |
| #6383 | S09b Decision: one assembly per context? |
| #6384 | S10 Data ownership per context (logical only) |
| #6385 | S11 `PublishDir` outside the project folder |
| #6386 | S12a Metrics controllers → read-side query ports per capability |
| #6387 | S12b Other wide controllers: split by capability or move logic to a query port |
| #6388 | S12c DTOs become pure mapping |
| #6389 | FE1 Import-boundary enforcement + ratcheting allow-list |
| #6390 | FE2 Package the frontend by module (pilot: Delivery) |
| #6391 | FE3 Shrink `src/models` |
| #6392 | FE4 Per-module API ports |
| #6393 | FE5 Split wide units |
| #6394 | FE6 "Project" → Portfolio naming in the frontend |
| #6395 | S13 Aggregate boundaries in persistence |

## Wave: DISCUSS / [REF] Improvement log

One entry per item closed, removed or gated, newest last. Read it at every pick-up: it is where a lesson from one
item changes the plan for the others.

### 2026-10-29 — S04a #6367 Close the `Models ↛ Services` gap — Done
- Commits: `e3ac351d4..93139eca0` (shared with S05a) · archive: `docs/evolution/2026-10-29-stories-6367-6369-kernel-and-repository-ratchets.md`
- Ratchet: `Models` does not depend on `Services`, exception list **empty**; the update-queue rule lost its `UpdateType` exception.
- KPIs re-measured: KPI 3 `Models` files 246 → 252 (six kernel types moved in; S04b brings it down).
- Changes the plan: the brief's two-type gap was eight types (seven moved, `LegacyDefaultEncryptionKey` inverted into a `Services` extension). New ADR-268 (ratchet convention); ADR-027 status note. Dependency rules now read the compiled IL (`IlDependencies`, Mono.Cecil), because ArchUnitNET drops edges written inside `async` bodies in the Release build CI tests: later dependency ratchets (S02, S03*, S04b, S09a) should use `IlDependencies` for type-dependency questions, not ArchUnitNET's graph alone.

### 2026-10-29 — S05a #6369 Ratchet guard: `GetAllByPredicate` callers + `API` on repositories — Done
- Commits: `e3ac351d4..93139eca0` (shared with S04a) · archive: `docs/evolution/2026-10-29-stories-6367-6369-kernel-and-repository-ratchets.md`
- Ratchet: `GetAllByPredicate` called only from **15** listed files (`file:line` on failure); the `API` namespace depends on a repository only in **23** listed types (20 controllers + 3 helpers). Both fail on a newcomer and on a stale entry.
- KPIs re-measured: KPI 4 14 → 15 files, now guarded (`ReportOwnerLookup` from Epic 6350 arrived between DESIGN and landing); KPI 5 ≈20 → 23 types, now guarded; KPI 12 mutation 91.30 % on touched code.
- Changes the plan: scope widened from controllers to the whole `API` namespace (maintainer, 2026-10-27), so S05c–e and S12* shrink a 23-entry list. `ProductionSource` is the shared source-scan helper; moving the five private copies onto it is an open housekeeping follow-up, not yet in the catalogue. FE1's brief now weighs Fallow as a tool candidate (`6530cab2d`).
