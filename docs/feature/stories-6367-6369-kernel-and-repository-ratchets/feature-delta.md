# Feature: stories-6367-6369-kernel-and-repository-ratchets

Improvement batch from Epic #6264 (path to a modular monolith), picked up 2026-10-27 through
`/continuous-improvement`.

| ADO | Item | Brief |
|---|---|---|
| #6367 | S04a Close the `Models ↛ Services` gap (S) | [`slice-11-s04a-close-models-services-gap.md`](../epic-6264-modular-monolith/slices/slice-11-s04a-close-models-services-gap.md) |
| #6369 | S05a Ratchet guard: `GetAllByPredicate` callers + controllers on repositories (S) | [`slice-13-s05a-ratchet-getallbypredicate-and-controller-repos.md`](../epic-6264-modular-monolith/slices/slice-13-s05a-ratchet-getallbypredicate-and-controller-repos.md) |

Both are G0 backend ArchUnit ratchets with no dependencies, in the same test suite
(`Lighthouse.Backend.Tests/Architecture/`), so they share one DESIGN.

**DISCUSS: covered at Epic level** (Epic Wave Decisions D-6264-3, D-6264-4); waves start at DESIGN.

## Inherited [REF]

From [`../epic-6264-modular-monolith/feature-delta.md`](../epic-6264-modular-monolith/feature-delta.md):

- DDD vocabulary used in this Epic
- How to pick up a Story
- Cross-Cutting Impact Checklist
- Outcome KPIs + guardrails (KPI 3 `Models` file count, KPI 4 `GetAllByPredicate` files, KPI 5 controllers on repositories)
- Wave Decisions (Key Decisions, Constraints)
- Risks / Open questions for per-Story DESIGN

No ADR from S01 exists yet; S05a's "persistence adapters" therefore means
`Services/Implementation/Repositories/**` (brief, Notes for DESIGN).

## Wave: PICK-UP / [REF] DoR re-run (2026-10-27)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement, domain language | PASS | Both briefs name the smell: the documented `Models ↛ Services` gap (`ModuleBoundariesArchUnitTest.SharedKernel_DoesNotDependUpwardOnApi` carries it as prose), and an unguarded `GetAllByPredicate` / repository-in-controller leak. |
| 2 Persona | PASS | `lighthouse-maintainer` + the CI build. |
| 3 ≥ 3 domain examples, real data | PASS | Real file lists below, re-counted today. |
| 4 UAT Given/When/Then | PASS | S04a: 3 AC, S05a: 4 AC, both G/W/T-phrased; full scenarios at DISTILL. |
| 5 AC derived from UAT | PASS | 1:1 with each brief's Elevator Pitch "After". |
| 6 Right-sized | PASS | S + S; carpaccio at DESIGN. |
| 7 Technical notes + cross-cutting | PASS | See checklist below; Epic answers stand (all N/A, behaviour-free). |
| 8 Dependencies tracked | PASS | None for either item. |
| 9 Outcome KPIs, measurable | PASS | KPIs 3, 4, 5; re-counted below. |

**S04a's hypothesis is disproved at pick-up** (see re-count): the gap is held open by more than the two
named types. Per the brief, DESIGN decides per type whether to move it into the kernel or baseline its
file by inline list, and records each one.

## Wave: PICK-UP / [REF] Baseline re-count (2026-10-27)

### S04a — `Models` → `Services`

| Measure | Brief (2026-10-22) | Today |
|---|---|---|
| `Models` files (KPI 3) | 242 | **246** |
| Types keeping the gap open | 2 (`IEntity`, `WorkTrackingSystems`) | **≥ 6**; 39 `Models` files import a `Services` namespace |
| `IEntity` location | `Services/Interfaces/IEntity.cs` | unchanged |
| `WorkTrackingSystems` location | `Services/Implementation/WorkTrackingConnectors/WorkTrackingSystems.cs` | unchanged |

`Services` types `Models` uses today (from `using` lines plus type-name matches; the ArchUnit rule names
the exact type-level list at DESIGN):

- `IEntity` (`Services.Interfaces`): ~30 entity files.
- `IConcurrencyTokenEntity` (`Services.Interfaces`): ApiKey, RbacGroupMapping, UserProfile, Delivery,
  Report, WorkTrackingSystemConnection, WorkTrackingSystemOptionsOwner. **Not in the brief.**
- `WorkTrackingSystems` (`Services.Implementation.WorkTrackingConnectors`): WorkTrackingSystemConnection,
  SupportedConnectionDefaults.
- `LegacyDefaultEncryptionKey` (`Services.Implementation.Encryption`): EncryptionKeyRing. **Not in the brief.**
- `UpdateType` (`Services.Implementation.BackgroundServices.Update`): RefreshSubject. **Not in the brief.**
- `ReportOwner` (`Services.Interfaces.Reports`): Report. **Not in the brief.**
- Unresolved `using Lighthouse.Backend.Services.Implementation` / `.Interfaces.Reports` in
  ForecastPercentile, Delivery, Feature, ReportReadModels: either an unused `using` or a type outside the
  namespace's root folder. DESIGN resolves.

### S05a — `GetAllByPredicate` outside `Services/Implementation/Repositories/**` (KPI 4)

**14 files, 53 calls**: unchanged in total since 2026-10-22, with one shift: FeaturesController 3 → 2,
DemoBlockedHistoryBackfillHandler 1 → 2.

TeamMetricsService 14 · PortfolioMetricsService 13 · ConnectionSaveService 5 · WorkItemService 4 ·
WorkTrackingSystemConnectionUsage 3 · RefreshLogService 2 · DemoPercentilesBackfillHandler 2 ·
DemoBlockedHistoryBackfillHandler 2 · OverTimeFillTarget 2 · FeaturesController 2 ·
WriteBackTriggerService 1 · RefinementList 1 · BlockedCountSeriesBuilder 1 · TeamMetricsController 1

### S05a — `API` files referencing `IRepository<>` / `I*Repository` (KPI 5)

**20 controllers**, exactly the brief's list (whole-file search, so multi-line constructors are covered):
Deliveries, DeliveryNotes, DeliveryRules, DeliverySources, Demo, Features, Forecast,
ForecastRealityCheck, OptionalFeatures, Portfolio, PortfolioMetrics, Portfolios, Suggestions, Team,
TeamFeatureSizeMetrics, TeamMetrics, Teams, Wizards, WorkTrackingSystemConnection,
WorkTrackingSystemConnections.

**Plus 3 non-controller `API` files**: `APIHelpers.cs`, `Helpers/OwnerSettingRefusals.cs`,
`Helpers/PortfolioExtensions.cs`. DESIGN decides whether the rule covers controllers only or the whole
driving adapter.

## Wave: PICK-UP / [REF] Checklist (tick through DELIVER)

Continuous-improvement rules:

- [ ] Behaviour-free: routes, DTOs, serialized responses, pixels and topologies unchanged; existing tests green unmodified (apart from tests that move with their code). `WorkTrackingSystems` keeps its integer values.
- [ ] Ratchet in the same change: `Models ↛ Services`, the `GetAllByPredicate` guard and the controller-on-repository rule each land in the commit range that makes them pass. Inline file lists, never counts. Nothing loosened.
- [ ] Moves separate from edits: the type relocations are pure `refactor(...): move …` commits.
- [ ] Baselines re-counted (above) and re-checked at the PR.
- [ ] Mutation last, on frozen code; touched area not lower.
- [ ] `ARCHITECTURE.md` §4 gap note deleted; ADRs updated where a decision changes.
- [ ] Payoff claim: N/A, because no performance claim is made.
- [ ] No release notes (behaviour-free) unless the maintainer says otherwise.

Epic per-Story checklist (How to pick up a Story):

- [ ] Ratchet in the same PR; baselines are inline file lists; never loosened.
- [ ] Behaviour-free; API, E2E and response tests green unmodified.
- [ ] Mutation score in the touched area does not drop.
- [ ] Moves separate from edits.
- [ ] ARCHITECTURE.md updated in the same change (§4).
- [ ] One context at a time (N/A, because these items move no context; they add rules).
- [ ] Terminology follows the seeded defaults (no S01 glossary yet).
- [ ] Cross-cutting re-check: RBAC, Lighthouse-Clients, Website, usage-data event, UI sketch, each answered for this batch.
