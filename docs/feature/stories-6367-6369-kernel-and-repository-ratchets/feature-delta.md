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

## Wave: DESIGN / [REF] Decisions (2026-10-27, propose mode, maintainer decided)

Paths are under `Lighthouse.Backend/Lighthouse.Backend/` (production) and `Lighthouse.Backend/Lighthouse.Backend.Tests/` (tests).

| # | Decision | Verdict |
|---|---|---|
| D1 | S04a: move or baseline | Move six kernel-level types into `Models`; invert one. `Models ↛ Services` starts with an **empty** exception list. |
| D2 | `LegacyDefaultEncryptionKey` | Not kernel-level (it needs the classifier, envelope and ring holder, all `Services`). `EncryptionKeyRing.WithLegacyDefault()` becomes an extension method in `Services/Implementation/Encryption`; call syntax unchanged. |
| D3 | `UpdateType` exception | Dropped from `ModelsDoNotDependOnTheUpdateQueueArchUnitTest`; the test stays, narrower message and all. |
| D4 | S05a detection | Hybrid. `GetAllByPredicate` callers: source scan, inline file list, failure names file:line. Repository dependency: ArchUnitNET on type dependencies. |
| D5 | Repository targets | Match by name (`IRepository<>`, `^I\w*Repository$`) plus every type in `Services.Implementation.Repositories`. Not by the `Services.Interfaces.Repositories` namespace, which also holds `IFeatureListReader`, `IUserProfileLookup` and `RecordableDeliveries`. |
| D6 | Rule scope | The whole `API` namespace, 23 entries (20 controllers + 3 helpers). "Depends" means any type dependency, including generic arguments such as `GetRequiredService<IRepository<Team>>()`. Nested compiler types normalise to their declaring type. |
| D7 | Test placement | S04a extends `ModuleBoundariesArchUnitTest`; S05a creates one new class for both rules. |
| D8 | Shared source-scan helper | One new internal `ProductionSource` used by the new test only. Migrating the five private copies is a separate follow-up. |
| D9 | ADR | New ADR-268 for the ratchet convention; status note on ADR-027. |

## Wave: DESIGN / [REF] Component decomposition

| Path | Change | Note |
|---|---|---|
| `Services/Interfaces/IEntity.cs`, `IConcurrencyTokenEntity.cs` | Moved to `Models`, namespace `Lighthouse.Backend.Models` | ~55 files touched; in `Models` it deletes a `using` |
| `Services/Implementation/WorkTrackingConnectors/WorkTrackingSystems.cs` | Moved to `Models`, same namespace | integer values kept |
| `Services/Interfaces/Reports/ReportOwner.cs` (holds `ReportOwnerKind` too) | Moved to `Models/Reports`, namespace `Lighthouse.Backend.Models.Reports` | ~43 files |
| `Services/Implementation/BlackoutDaysExtensions.cs` | Moved to `Models`, namespace `Lighthouse.Backend.Models` | 17 files |
| `Services/Implementation/BackgroundServices/Update/UpdateType.cs` | Moved to `Models`, namespace `Lighthouse.Backend.Models` | ~70 files, mostly `using` lines |
| `Models/Encryption/EncryptionKeyRing.cs`, `Services/Implementation/Encryption/LegacyDefaultEncryptionKey.cs` | Edit: instance method becomes an extension | callers keep the syntax |
| `Tests/Architecture/ModuleBoundariesArchUnitTest.cs` | Edit: add `SharedKernel_DoesNotDependOnServices`, delete the prose caveat | |
| `Tests/Architecture/ModelsDoNotDependOnTheUpdateQueueArchUnitTest.cs` | Edit: drop the `UpdateType` exception and its doc paragraph | |
| `Tests/Architecture/ProductionSource.cs` | New, internal | finds production sources, strips comments |
| `Tests/Architecture/RepositoryLeakRatchetArchUnitTest.cs` | New | both S05a rules, one two-way comparison helper |
| `ARCHITECTURE.md` §4 (line 137) | Edit: delete the gap note | same change as the rule |

## Wave: DESIGN / [REF] Reuse analysis

| Existing component | Evidence | Decision |
|---|---|---|
| `ModuleBoundariesArchUnitTest` | has `ServiceLayerPattern`, `SharedKernelPattern`, and carries the caveat | EXTEND |
| `ModelsDoNotDependOnTheUpdateQueueArchUnitTest` | holds the only `UpdateType` exception | EXTEND (edit) |
| `LighthouseArchitecture.Production` | shared loaded architecture | REUSE |
| `ProductionSourceFiles` / `ProductionSourceOf` / `ProductionRoot` | private copies in `OverTimeReconstructionSeamArchUnitTest`, `SecretReadPathSourceStructureTest`, `SecretRotationWriteSurfaceTest`, `CalendarDayAnchorSeamArchUnitTest`, `JiraRouteLiteralsTest` | CREATE one shared helper rather than a sixth copy; migrating the five is out of scope |
| Baseline comparison | existing idiom is `Is.EqualTo` on an exact list, with one message for both directions | CREATE a small comparison with separate "new offender" and "stale entry" messages, inside the new class |
| Ratchet test class | nothing existing is about repository leaks | CREATE |
| `OverTimeReconstructionSeamArchUnitTest.OnlyTheReconciler_AsksTheFillerForDays` | the closest source-scan analogue | pattern reused, code not shared |

## Wave: DESIGN / [REF] Slices

Each is at most one day. Pure-move commits change namespaces and `using` lines only; edit commits change logic or tests. Every commit leaves the build green. The two stories are independent.

### S04a (Story 6367)

| Slice | Commits | Satisfies |
|---|---|---|
| S04a-1 | move: `IEntity` + `IConcurrencyTokenEntity` | AC: kernel types live in `Models` |
| S04a-2 | move: `WorkTrackingSystems` (up to ~276 files, mechanical; IDE0005 or compile-driven) | same |
| S04a-3 | move: `ReportOwner` + `ReportOwnerKind`; separate move: `BlackoutDaysExtensions` | same |
| S04a-4 | move: `UpdateType`; edit: drop the exception in `ModelsDoNotDependOnTheUpdateQueueArchUnitTest` | same |
| S04a-5 | edit: `WithLegacyDefault` becomes a `Services` extension | `Models` no longer reaches the encryption service |
| S04a-6 | edit: add `Models ↛ Services` with an empty exception list, delete the prose caveat, delete the `ARCHITECTURE.md` §4 gap note | AC: the rule fails on a new `Models → Services` dependency; the gap note is gone |

Before S04a-6 goes green, write the rule first and compare ArchUnitNET's offender list with the eight types in the evidence table below; any extra type is a ninth decision.

### S05a (Story 6369)

| Slice | Commits | Satisfies |
|---|---|---|
| S05a-1 | edit: `ProductionSource` helper and the `GetAllByPredicate` source scan with its 14-file list | AC: a new caller fails naming file:line; a listed file that stops calling fails until removed |
| S05a-2 | edit: the `API` repository-dependency guard with its 23-entry list. **First spike** how ArchUnitNET attributes lambda display classes and async state machines, and normalise nested types to the declaring type. | AC: the same two behaviours for the `API` rule |

Baselines to put in the lists (re-count at the PR):
- 14 files: `TeamMetricsService`, `PortfolioMetricsService`, `ConnectionSaveService`, `WorkItemService`, `WorkTrackingSystemConnectionUsage`, `RefreshLogService`, `DemoPercentilesBackfillHandler`, `DemoBlockedHistoryBackfillHandler`, `OverTimeFillTarget`, `FeaturesController`, `WriteBackTriggerService`, `RefinementList`, `BlockedCountSeriesBuilder`, `TeamMetricsController`.
- 23 `API` entries: the 20 controllers listed above, plus `APIHelpers`, `OwnerSettingRefusals`, `PortfolioExtensions`.

## Wave: DESIGN / [REF] S04a evidence per type

`Models` imports a `Services` namespace in 39 files, and has no fully qualified references or global usings, so the list is complete: 33 files for the two markers, plus `SupportedConnectionDefaults`, `EncryptionKeyRing`, `RefreshSubject`, `ReportReadModels`, `Feature`, `ForecastPercentile`.

| Type | What it is | Verdict |
|---|---|---|
| `IEntity`, `IConcurrencyTokenEntity` | markers; 33 `Models` types implement them | move |
| `WorkTrackingSystems` | dependency-free enum on the connection entity | move |
| `ReportOwner`, `ReportOwnerKind` | dependency-free value object and enum, used by `Report` and `ReportReadModels` | move |
| `BlackoutDaysExtensions` | extension methods over `BlackoutPeriod` and `ProcessBehaviourChart` only; used by `Delivery`, `Feature`, `ForecastPercentile` (the "unresolved usings") | move |
| `UpdateType` | five-member enum, used by `RefreshSubject` | move |
| `LegacyDefaultEncryptionKey` | depends on `SecretStateClassifier`, `SecretEnvelope`, `EncryptionKeyRingHolder` | invert, do not move |

Behaviour-freedom evidence:
- **Migrations:** both model snapshots contain no `Services` fully qualified name and no `HasConversion`; the migration files reference none of these types. EF stores the enum as an int. No `PendingModelChanges` and no snapshot churn expected, because no entity namespace changes.
- **Serialization and reflection:** no `Type.GetType`, `AssemblyQualifiedName`, `TypeNameHandling` or `JsonPolymorphic` touches them. Enums serialize by member name, and `UpdateType` is persisted only as an int in the advisory lock key, whose member values do not change. Swagger uses short type names, and none of the moved names clashes with an existing type. DataProtection does not use type names.

## Wave: DESIGN / [REF] Changed Assumptions

Quoted from `docs/feature/epic-6264-modular-monolith/slices/slice-11-s04a-close-models-services-gap.md`, as written at DISCUSS:

> Relocate `IEntity` (`Services/Interfaces/IEntity.cs`) and `WorkTrackingSystems` (`Services/Implementation/WorkTrackingConnectors/WorkTrackingSystems.cs`) into `Models`.

**Disproved: the two-type hypothesis.** Eight types keep the gap open: `IEntity`, `IConcurrencyTokenEntity`, `WorkTrackingSystems`, `ReportOwner`, `ReportOwnerKind`, `BlackoutDaysExtensions`, `UpdateType` and `LegacyDefaultEncryptionKey`. Seven move; `LegacyDefaultEncryptionKey` is inverted.

Quoted from `docs/feature/epic-6264-modular-monolith/feature-delta.md`, KPI table row 3:

> `Models` file count | brief ~218 → **242** | only S01's kernel allow-list

**Changed:** S04a adds six files to `Models` (`IEntity`, `IConcurrencyTokenEntity`, `WorkTrackingSystems`, `ReportOwner`, `BlackoutDaysExtensions`, `UpdateType`), so the count rises by 6 until S04b reduces it. The KPI row now says so.

Quoted from the same table, KPI row 5:

> Controllers depending on `IRepository<>` / `I*Repository` | brief 5 → **≈20** (re-count at S05a DESIGN)

**Changed:** the scope is the `API` driving adapter, not controllers only: 20 controllers plus 3 helpers (`APIHelpers`, `OwnerSettingRefusals`, `PortfolioExtensions`) make 23, because a helper taking a repository is the same leak.

## Wave: DESIGN / [REF] Risks

- The `Models` file count (KPI 3) goes up before it comes down; recorded in the Epic table.
- Stale `using` lines may fail the build if Sonar S1128 is enforced; remove them with tooling in each move commit.
- ArchUnitNET's handling of compiler-generated nested types is unverified; spiked first in S05a-2. Fallback: a source scan for the same list.
- The source scan is textual: a string or alias could fool it. Comments are stripped; the risk is accepted.
- A large mechanical diff can hide a logic edit; that is why moves and edits are separate commits.
- Renaming or moving a listed file touches the baseline list.

## Wave: DESIGN / [REF] Open questions for DISTILL and DELIVER

- DISTILL: the scenarios are architecture-test scenarios (a new offender fails and names itself; a stale entry fails and names itself) for each of the three rules. No UI is touched, so no sketch is owed.
- DELIVER: propose the rigor profile per slice and record it here before the first dispatch.
- DELIVER: confirm ArchUnitNET's offender list for `Models → Services` equals the eight types before the rule commit.
- DELIVER: confirm the exact `Services.Implementation.Repositories` type list is empty for `API` today (expected, the 23 entries are all interface dependencies).
- Follow-up (not this batch): migrate the five private source-scan helpers to `ProductionSource`.
