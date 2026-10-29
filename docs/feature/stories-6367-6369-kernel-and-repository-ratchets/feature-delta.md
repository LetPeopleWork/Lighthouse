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
| 2 Persona | PASS | `lighthouse-maintainer`, who today can add a `Models → Services` dependency or a new `GetAllByPredicate` caller with nothing failing; and the CI build that will fail instead. |
| 3 ≥ 3 domain examples, real data | PASS | Real file lists below, re-counted today. |
| 4 UAT Given/When/Then | PASS | S04a: 3 AC (2 G/W/T, 1 behaviour constraint); S05a: 4 AC (2 G/W/T, 2 constraints). The constraints become scenarios at DISTILL. |
| 5 AC derived from UAT | PASS | 1:1 with each brief's Elevator Pitch "After". |
| 6 Right-sized | PASS | Briefs size both S. DESIGN found S04a is nearer M (eight types, six slices, most of them mechanical moves); every slice is still ≤ 1 day. |
| 7 Technical notes + cross-cutting | PASS | Cross-cutting answered for this batch in the checklist below (all N/A, with reasons). |
| 8 Dependencies tracked | PASS | Neither item has a hard or soft dependency (graph and both briefs agree). S04a is a hard dependency of S04b; S05a of S05b–S05f. |
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

**14 files, 52 calls** (2026-10-22 said 53): FeaturesController 3 → 2. One match in
DemoBlockedHistoryBackfillHandler is inside a comment, so it has one call, not two.

TeamMetricsService 14 · PortfolioMetricsService 13 · ConnectionSaveService 5 · WorkItemService 4 ·
WorkTrackingSystemConnectionUsage 3 · RefreshLogService 2 · DemoPercentilesBackfillHandler 2 ·
OverTimeFillTarget 2 · FeaturesController 2 ·
DemoBlockedHistoryBackfillHandler 1 · WriteBackTriggerService 1 · RefinementList 1 · BlockedCountSeriesBuilder 1 · TeamMetricsController 1

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

- [x] Behaviour-free: routes, DTOs, serialized responses, pixels and topologies unchanged; existing tests green unmodified (apart from tests that move with their code). `WorkTrackingSystems` keeps its integer values. *Checked at close-out 2026-10-29: outside `Tests/Architecture` the range `e3ac351d4..93139eca0` changes only `using` lines in existing tests, plus new kill-round tests; CI 40563773352 green.*
- [x] Ratchet in the same change: `Models ↛ Services`, the `GetAllByPredicate` guard and the controller-on-repository rule each land in the commit range that makes them pass. Inline file lists, never counts. Nothing loosened. *`af69c31a0`, `06641a5ba`, `5e3a25dc1`; lists of 0, 15 and 23 entries.*
- [x] Moves separate from edits: the type relocations are pure `refactor(...): move …` commits. *`d39e4bf33`, `93c00f05d`, `ff78469e6`, `d66e6d25f`, `bb0883096`.*
- [x] Baselines re-counted (above) and re-checked at the PR. *`ReportOwnerLookup` from Epic 6350 joined the `GetAllByPredicate` list at landing (14 → 15).*
- [x] Mutation last, on frozen code; touched area not lower. *91.30 % after the kill round, see `mutation/results.md`.*
- [x] `ARCHITECTURE.md` §4 gap note deleted; ADRs updated where a decision changes. *`4a3d55367`, `21480424b`; ADR-268 new.*
- [x] Payoff claim: N/A, because no performance claim is made.
- [x] No release notes (behaviour-free) unless the maintainer says otherwise. *Neither ADO item carries a tag.*

Epic per-Story checklist (How to pick up a Story):

- [x] Ratchet in the same PR; baselines are inline file lists; never loosened.
- [x] Behaviour-free; API, E2E and response tests green unmodified.
- [x] Mutation score in the touched area does not drop.
- [x] Moves separate from edits.
- [x] ARCHITECTURE.md updated in the same change (§4).
- [x] One context at a time (N/A, because these items move no context; they add rules).
- [x] Terminology follows the seeded defaults (no S01 glossary yet).
- [x] Cross-cutting re-check, answered 2026-10-27: RBAC N/A, because no endpoint or permission changes. Lighthouse-Clients N/A, because no payload changes (enums serialise by member name). Website N/A, because nothing user-visible changes. Usage-data event N/A, because the batch is behaviour-free (DEVOPS). UI sketch N/A, because no UI is touched.

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
| `Services/Implementation/WorkTrackingConnectors/WorkTrackingSystems.cs` | Moved to `Models`, namespace `Lighthouse.Backend.Models` | integer values kept; the namespace must change, or the rule still flags it |
| `Services/Interfaces/Reports/ReportOwner.cs` (holds `ReportOwnerKind` too) | Moved to `Models/Reports`, namespace `Lighthouse.Backend.Models.Reports` | ~43 files |
| `Services/Implementation/BlackoutDaysExtensions.cs` | Moved to `Models`, namespace `Lighthouse.Backend.Models` | 17 files |
| `Services/Implementation/BackgroundServices/Update/UpdateType.cs` | Moved to `Models`, namespace `Lighthouse.Backend.Models` | ~70 files, mostly `using` lines |
| `Models/Encryption/EncryptionKeyRing.cs`, `Services/Implementation/Encryption/LegacyDefaultEncryptionKey.cs` | Edit: instance method becomes an extension | callers keep the call syntax but need a `using Lighthouse.Backend.Services.Implementation.Encryption` where they lack one (16 files hold calls, tests included) |
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
| Baseline comparison | `PostgresContainerSharingGuardTest`, `BackendTestParallelizationGuardTest` and `CalendarDayAnchorSeamArchUnitTest` already check both directions, as two tests with separate new-offender and stale-entry messages | CREATE one small comparison inside the new class, copying their message pattern; three rules sharing one helper beats six near-identical test pairs |
| Ratchet test class | nothing existing is about repository leaks | CREATE |
| `OverTimeReconstructionSeamArchUnitTest.OnlyTheReconciler_AsksTheFillerForDays` | the closest source-scan analogue | pattern reused, code not shared |

## Wave: DESIGN / [REF] Slices

Each is at most one day. Pure-move commits change namespaces and `using` lines only; edit commits change logic or tests. Every commit leaves the build green. The two stories are independent.

### S04a (Story 6367)

| Slice | Commits | Satisfies |
|---|---|---|
| S04a-1 | move: `IEntity` + `IConcurrencyTokenEntity` | AC: kernel types live in `Models` |
| S04a-2 | move: `WorkTrackingSystems` (up to ~276 files, mechanical; IDE0005 or compile-driven); edit (same commit range, separate commit): add `Models/WorkTrackingSystems.cs` to `SHARED_REGEX` in `Scripts/test-selection/path-classifier.sh`, with a scenario in `test-path-classifier.sh` (added at DEVOPS) | same, plus: a change to the enum still forces the live-connector suites |
| S04a-3 | move: `ReportOwner` + `ReportOwnerKind`; separate move: `BlackoutDaysExtensions` | same |
| S04a-4 | move: `UpdateType`; edit: drop the exception in `ModelsDoNotDependOnTheUpdateQueueArchUnitTest` | same |
| S04a-5 | edit: `WithLegacyDefault` becomes a `Services` extension, plus the `using` in the calling files that lack it | `Models` no longer reaches the encryption service |
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

**S04a AC1** ("Given the two types are in the kernel") now reads as: given the seven kernel-level types are in `Models` and `WithLegacyDefault` lives in `Services`. **S05a AC3** ("controllers may not depend on…") now covers the whole `API` namespace, 23 entries; maintainer decision 2026-10-27.

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

## Wave: DEVOPS / [REF] Platform decisions (2026-10-27)

Every platform decision is the project's existing one; this batch changes none of them. Behaviour-free
test-and-namespace work: no deployment, image, chart, configuration, migration or external call changes.

| Decision | Answer |
|---|---|
| Deployment target, orchestration, strategy | Unchanged: Standalone, Docker, Helm/cluster via the regular release pipeline. Nothing to deploy beyond the next release. |
| CI/CD platform | GitHub Actions, existing workflows. No new job, no new workflow. |
| Branching | Trunk-based on `main`. |
| Observability | N/A, because no runtime behaviour changes. No log line, metric or trace is added. |
| Continuous learning / flags | N/A, because there is nothing a user can see to roll out. |
| Mutation testing | Per-feature (CLAUDE.md), last, on frozen code. See below. |
| Usage-data event | **N/A, because the batch is behaviour-free**: nothing a user does changes, so there is no use to count. |

## Wave: DEVOPS / [REF] CI pipeline outline

- **Where the new rules run:** `Lighthouse.Backend.Tests/Architecture/` tests carry no `Integration` category, so
  `ci_backend.yml`'s always-on `Category!=Integration` part runs all three new rules on every push that touches the backend (`ci_changes.yml` sets `backend=true`). Locally:
  `dotnet test --filter FullyQualifiedName~Architecture` for the loop, and the CLAUDE.md filter before push.
- **Force-full exposure (finding):** `Scripts/test-selection/path-classifier.sh` treats top-level
  `Services/Implementation/WorkTrackingConnectors/*.cs` and `Models/WorkTrackingSystemConnection*` as shared connector paths.
  The push carrying **S04a-2** touches both (the enum's old file is deleted, the entity's `using` changes), plus `using`
  lines inside the Jira, ADO, Linear and ServiceNow folders. That push therefore runs every live-connector suite. Expected
  and accepted: one slow, more flake-exposed CI run, nothing to change in CI for it. The classifier edit in S04a-2 is not a
  behaviour change: it keeps today's trigger coverage of the enum, so CI wall time on later pushes is as it is today. Push the S04a moves together so it
  happens once, not per slice. Do not run the connector categories locally for a `using`-only change (the Linear key is
  shared with CI).
- **Classifier gap (finding → DELIVER step in S04a-2):** after the move the enum lives at `Models/WorkTrackingSystems.cs`,
  which `SHARED_REGEX` does not match, so a later change to the enum would no longer force the connector suites. S04a-2
  extends `SHARED_REGEX` with `^Lighthouse\.Backend/Lighthouse\.Backend/Models/WorkTrackingSystems\.cs$` and adds a
  scenario to `Scripts/test-selection/test-path-classifier.sh`, in the same commit range as the move.
- **Sonar:** move commits must drop stale `using`s (S1128 / IDE0005), or the gate reports new code smells.

## Wave: DEVOPS / [REF] Mutation testing scope

Stryker.NET on production code only (the new tests and `ProductionSource` are test code). Run once, last, after S05a-2,
on frozen code. Moved files' line ranges shift, so baselines are taken by file, not by line span.

| File | Why it is in scope | Gate |
|---|---|---|
| `Models/BlackoutDaysExtensions.cs` (moved) | Logic moved with its tests | Score not lower than at its old path |
| `Models/Encryption/EncryptionKeyRing.cs` (loses `WithLegacyDefault`) + `Services/Implementation/Encryption/LegacyDefaultEncryptionKey.cs` (gains the `WithLegacyDefault` extension; callers in `EncryptionKeyRingBootstrapper` and `SecretCustodyService` keep their syntax) | S04a-5 edits logic | ≥ 80% on `LegacyDefaultEncryptionKey.cs`; ring not lower |
| `Models/Reports/ReportOwner.cs` (moved) | Value object with logic | Not lower |
| Enums and marker interfaces (`IEntity`, `IConcurrencyTokenEntity`, `WorkTrackingSystems`, `UpdateType`) | No mutants of interest | N/A, because Stryker produces no meaningful mutants for them |

## Wave: DEVOPS / [REF] Environment matrix

See `environments.yaml`: one environment (`clean`, CI ubuntu-latest + the developer machine), since nothing installs or
runs differently. Coexistence: the Docker images, Helm chart, Standalone builds, Lighthouse-Clients and both migration
assemblies must not change; the latter is evidenced at DESIGN (no snapshot churn) and checked by `dotnet build` showing no
`PendingModelChanges`.

## Wave: DEVOPS / [REF] Changed Assumptions

Quoted from this file's DESIGN slice table, S04a-2 as written at DESIGN:

> move: `WorkTrackingSystems` (up to ~276 files, mechanical; IDE0005 or compile-driven)

**Changed:** S04a-2 also extends the CI path classifier, because the enum's move would otherwise drop it from the
connector-suite trigger. The slice row now says so.

## Wave: DISTILL / [REF] Scenario list

The "driving port" is the architecture test suite (`dotnet test --filter FullyQualifiedName~Architecture`); the
observable is pass or fail and the failure message, which names the offending file or type. Behaviour-freedom is
guarded by the existing suites staying green unmodified, so no scenario repeats it. Every new test is
`[Ignore("pending: <slice>")]`; DELIVER un-ignores one at a time. 31 new tests. Edge or error cases: 23 of 31 (74%). Strict count of tests that prove a violation fires and is named: 5 (a new
offender, a stale entry, both at once, and one offending fixture Model for each of the two kernel rules), plus 5 that prove a
dependency is detected in each shape (constructor, generic argument, concrete repository, lambda, async method).
FsCheck is not a test dependency and none was added, so the comparison and the scan are example-based.

Tags are NUnit `[Category]` (`stories-6367-6369`, `slice-s05a` on the new class). Kind: H happy, E error, B boundary or edge.

| Rule / unit | Scenario | Kind | Slice |
|---|---|---|---|
| `GetAllByPredicate` | called only from the 14 listed files, list unique | H | S05a-1 |
| comparison | a list matching the code passes | H | S05a-1 |
| comparison | a new offender fails, names itself and carries the fix hint | E | S05a-1 |
| comparison | a listed entry that no longer offends fails, names itself, says delete | E | S05a-1 |
| comparison | newcomer and stale entry at once: both named in one failure | E | S05a-1 |
| comparison | order and repeats do not matter | B | S05a-1 |
| comparison | nothing offending against an empty list passes | B | S05a-1 |
| source scan | a call in code is reported as file:line | H | S05a-1 |
| source scan | a mention in a line comment, and in a block comment, is not a call | B, B | S05a-1 |
| source scan | the declaration on `IRepository.cs` is not a call | B | S05a-1 |
| source scan | a call inside `Services/Implementation/Repositories/` is allowed | B | S05a-1 |
| `API` ↛ repositories | `API` depends on a repository only in the 23 listed types, list unique | H | S05a-2 |
| `API` ↛ repositories | a constructor parameter on `IRepository<>` is a dependency | H | S05a-2 |
| `API` ↛ repositories | `IRepository<>` named only as a generic argument (`GetRequiredService<IRepository<Team>>()`) is a dependency | E | S05a-2 |
| `API` ↛ repositories | a concrete repository class is a dependency | E | S05a-2 |
| `API` ↛ repositories | a lambda asking for a repository is reported against the type that wrote it | B | S05a-2 |
| `API` ↛ repositories | an async method asking for one is reported against the type that wrote it | B | S05a-2 |
| `API` ↛ repositories | `IFeatureListReader` (same namespace, not a repository) and an unrelated type are not dependencies | B | S05a-2 |
| normalisation | a real compiler-made nested type maps to its declaring type; a guard first proves such a type exists, so the test can fail | B | S05a-2 |
| normalisation | a nested type someone wrote (`Outer+Inner`) keeps its own name; a name-stripping helper fails this | B | S05a-2 |
| normalisation | an ordinary type name is left alone | B | S05a-2 |
| source scan | `//` and `/*` inside a string literal (a URL) do not hide a call later on the same line | B | S05a-1 |
| source scan | a call on the line after its receiver is reported at the call's line; a comment mentioning the name just above does not count | B | S05a-1 |
| `Models` ↛ `Services` | `SharedKernel_DoesNotDependOnServices`, empty exception list, against the real assembly | H/E | S04a-6 |
| `Models` ↛ `Services` | a made-up Model that reaches a made-up Services type fails the same rule and is named | E | S04a-6 |
| `Models` ↛ `Services` | a made-up Model that stays inside the kernel passes | H | S04a-6 |
| `Models` ↛ update queue | `UpdateType` exception removed: no Model depends on the update queue namespace | E | S04a-4 |
| `Models` ↛ update queue | made-up offending Model fails and is named; made-up clean Model passes | E, H | S04a-4 |
| path classifier | `Models/WorkTrackingSystems.cs` gives `connector_shared=true` | H | S04a-2, DELIVER step (below) |

The two-way proof for the real rules rests on the comparison tests: the real `GetAllByPredicate` and `API` tests run
the same `AssertMatchesBaseline`, so "a new offender fails and names itself" and "a stale entry fails and names itself"
hold for them through the pure unit. `Models ↛ Services` and `Models ↛ update queue` have no list, so only the first
half applies; each rule is defined once in `KernelRules` and run over the real assembly and over fixture types in the
test assembly (a second architecture loaded from them works with ArchUnitNET).

The stored integer values of `WorkTrackingSystems` are already pinned by an active test
(`ServiceNowConnectionConfigurationTest.ServiceNow_SitsAtTheEndOfTheStoredWorkTrackingSystemOrder`, all five members),
so none was added.

Same-reduction trap: the nested-type test takes its input from reflection over fixture types the compiler made and its
expected answer from `DeclaringType`, two independent routes, so a no-op normalisation fails it.

## Wave: DISTILL / [REF] Walking skeleton

N/A, because this is a behaviour-free ratchet batch with no user journey. What replaces it: the three real-code tests
(`GetAllByPredicate_IsCalledOnlyFromTheFilesOnTheList`, `TheApi_DependsOnARepositoryOnlyInTheTypesOnTheList`,
`SharedKernel_DoesNotDependOnServices`) each run the whole path from the source tree or the compiled assembly to a
verdict on the real code. They are the last test un-ignored in each rule's slice, once the baseline is true.

## Wave: DISTILL / [REF] Adapter coverage

The source tree (read through `ProductionSource`) and the compiled production assembly (read by ArchUnitNET) are the
only adapters, both real I/O. Real-code tests: `GetAllByPredicate_…` (source tree), `TheApi_…` and `SharedKernel_…`
(assembly). The synthetic scenarios use made-up source text and fixture types compiled into the test assembly
(`RatchetFixtures`, loaded into their own architecture model, never mixed with `LighthouseArchitecture.Production`).
They cannot model a string or alias fooling the textual scan (accepted at DESIGN), nor ArchUnitNET's handling of
production types beyond what the fixtures reproduce.

## Wave: DISTILL / [REF] Scaffolds

All marked `// SCAFFOLD: true`, all throw `AssertionException` (RED, not BROKEN), all compile.

| Scaffold | Where | Built in |
|---|---|---|
| `ProductionSource.Files()`, `ProductionSource.WithoutComments()` (the `ProductionSourceFile` record is real) | `Architecture/ProductionSource.cs`, new, internal | S05a-1 |
| `AssertMatchesBaseline(actual, baseline, fixHint)` | private static in `RepositoryLeakRatchetArchUnitTest` | S05a-1 |
| `GetAllByPredicateCallSites(sources)`, returns `path:line` | same | S05a-1 |
| `RepositoryDependentsIn(architecture, namespacePattern)`, returns normalised full type names | same | S05a-2 |
| `DeclaringTypeOf(typeFullName)`: compiler-made nested names map to the declaring type, user-written nested types keep their name | same | S05a-2 |
| `KernelRules.ModelsDoNotDependOnServices(models, services)` and `KernelRules.ModelsDoNotDependOnTheUpdateQueue(models, updateQueue)`; return the rule with its `Because` text; the namespace patterns are the constants already on the class | `Architecture/KernelRules.cs`, new, internal | S04a-6, S04a-4 |

`SharedKernel_DoesNotDependOnServices` and the update-queue test now call the `KernelRules` factories, so they are red
through the scaffold; with a probe body they fail on the real code as listed in `red-classification.md`. The fixture
types (`Lighthouse.Backend.Models.KernelFixtures.*`, `Lighthouse.Backend.Services.KernelFixtures.AServiceType`) are final.
The two baseline lists (14 files, 23 types) are already filled in from today's tree, and the
`RatchetFixtures` types are final. The helpers are private statics in the test class; if DELIVER prefers a reusable
type, moving `AssertMatchesBaseline` out is a refactor with the tests unchanged.

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/`, as DESIGN placed it: new `RepositoryLeakRatchetArchUnitTest.cs`
`ProductionSource.cs`, `KernelRules.cs` and `KernelRulesCatchANewOffenderArchUnitTest.cs`; edits to `ModuleBoundariesArchUnitTest.cs`
and `ModelsDoNotDependOnTheUpdateQueueArchUnitTest.cs`.
Pending reasons have the form `pending: <slice id>`. RED evidence: `distill/red-classification.md`. At hand-off
`dotnet test --filter FullyQualifiedName~Architecture` is 238 passed, 31 skipped, 0 failed.

## Wave: DISTILL / [REF] UI sketches

N/A, because nothing user-visible changes.

## Wave: DISTILL / [REF] Open items for DELIVER

- **S04a-2, classifier scenario (not scaffolded):** `Scripts/test-selection/test-path-classifier.sh` is a flat list of
  `assert` lines with no skip idiom, so a pending scenario would turn the script red. In the S04a-2 commit range add
  `diff_work_tracking_systems=$'Lighthouse.Backend/Lighthouse.Backend/Models/WorkTrackingSystems.cs'` and
  `assert "work-tracking-systems: shared=true" "$(classify_shared "$diff_work_tracking_systems")" "true"` next to the
  "shared models" scenario. Add the assert first and see it fail, then extend `SHARED_REGEX`.
- **S04a-4:** once the exception is gone from `TheDomainModel_KnowsNothingOfTheUpdateItRunsUnder`, it and the new
  `…NotEvenTheNameOfAnUpdateType` are the same rule; delete one. The doc paragraph about `UpdateType` goes too.
- **S04a-4, one rule, one definition:** replace the active test (it re-declares its own patterns and keeps
  `AreNot(typeof(UpdateType))`) with the `KernelRules.ModelsDoNotDependOnTheUpdateQueue()` one and delete the duplicate. The
  active test stays untouched until `UpdateType` has moved, so it stays green.
- **S04a-6:** the offender list was compared at DISTILL (with a probe body): it names exactly the eight types DESIGN lists,
  so no ninth decision today; re-check at the rule commit. Put the rule's `Because` text in the `KernelRules` factory, and
  delete the prose caveat in `SharedKernel_DoesNotDependUpwardOnApi`'s `.Because(...)`.
- **S05a-1:** implement `ProductionSource` (the comment-blanking idea is in `CalendarDayAnchorSeamArchUnitTest`; keep
  line breaks so line numbers hold). The scan must fail when it finds no sources.
- **S05a-2:** spike first how ArchUnitNET attributes lambda display classes and async state machines; the fixtures cover
  both. If it attributes them to the nested type, `DeclaringTypeOf` maps them; if it drops them, the fallback is a
  source scan (DESIGN risk). Confirm the `Services.Implementation.Repositories` branch is empty for `API` today.
- **Whole batch:** the negative tests (`…IsNotACall`, `…IsNotADependency`, `…Passes`) are red today only through the
  stub; run each once against a deliberately over-reporting helper to see it fail.
- **Environmental:** in a worktree 2 Licensing tests fail without the gitignored licence fixture; the live-connector
  categories are not run.

## Wave: DELIVER / [REF] Rigor profile (maintainer decision 2026-10-27)

| Slices | Profile | Crafter model | Reviewer model |
|---|---|---|---|
| S04a-1, S04a-2, S04a-3, S04a-4, S04a-5, S04a-6, S05a-1 | standard | sonnet | haiku |
| S05a-2 | thorough | opus | sonnet |

Pace: all eight slices, refactor + review after each, then final review and mutation, then hold for the maintainer's review. Nothing is pushed.

## Wave: DELIVER / [REF] ArchUnitNET spike finding (02-04)

Probed with a throwaway test (not committed), the fixtures loaded into their own architecture and the production
architecture loaded separately. Run in both build configurations, because CI runs `dotnet test -c Release`.

- **a. Lambda.** ArchUnitNET loads no compiler-made types at all: zero types whose name holds `<`, in the fixture
  architecture and in the whole production architecture. The lambda's edge is folded onto the type that wrote it:
  `RatchetFixtures.LambdaDependent` reports `BodyTypeMember -> IRepository`1`, in Debug and in Release.
- **b. Async method.** In Debug the state machine's edge is folded onto the writing type too:
  `RatchetFixtures.AsyncDependent` reports `BodyTypeMember -> IRepository`1`. **In Release the edge is gone**:
  `AsyncDependent` reports no repository dependency at all. A dependency written only inside an `async` body is
  invisible to ArchUnitNET in the build CI runs (the ledger's "edges inside `async` bodies exist in Debug only").
- **c. Generic argument.** Yes: `RatchetFixtures.GenericArgumentDependent` (`GetRequiredService<IRepository<Team>>()`)
  reports `BodyTypeMember -> IRepository`1`, in Debug and in Release.
- **d. Real code.** `API.ApiHelpers.GetEntityByIdAndExecuteActionInternal` is `async` and calls `repo.GetById(id)`. In
  Debug ArchUnitNET reports `MethodCall -> IRepository`1` on `ApiHelpers`, origin member that method; in Release that
  edge disappears and only the parameter's `MethodSignature` edge is left. The fixture result holds on real code.
  Today all 23 API types also carry a constructor or parameter edge, so the Release set is still the 23 by luck, not
  by design. No API type depends on anything in `Services.Implementation.Repositories`; the hits in
  `Services.Interfaces.Repositories` beyond the list are `IUserProfileLookup` (`EmbedEntryController`) and
  `FeatureParent` (`FeatureListRows`), service-contract types the rule leaves out.
- **Separator.** ArchUnitNET writes a nested type as `Outer+Inner` (`RatchetFixtures.OuterWithAnInnerType+Inner`), the
  same `+` as reflection's `FullName`; `DeclaringTypeOf` splits on `+`.
- **Naming.** The type is `Lighthouse.Backend.API.ApiHelpers` (file `APIHelpers.cs`); the list says `…API.APIHelpers`,
  which the ordinal comparison will report as both a newcomer and a stale entry. Fix the entry in 02-05.

**Route for 02-05: the DESIGN FALLBACK, a source scan through `ProductionSource`** for the same 23-entry list, because
in the Release build CI runs, ArchUnitNET cannot see a repository asked for inside an `async` method, and a ratchet that
only works in Debug lets a newcomer through in CI. The pending fixture tests call
`RepositoryDependentsIn(ArchitectureModel, …)` on compiled fixtures, so they need re-shaping for source text before
02-05 starts; that is a test-design change for the acceptance designer. `DeclaringTypeOf` stays built and tested, but
the source-scan route does not need it.

## Wave: DELIVER / [REF] Route for the API repository ratchet (02-05)

ArchUnitNET's dependency graph was set aside: in the Release build CI tests, it loses a repository used inside an `async` body.
The design's textual fallback was not taken either: finding the type that wrote a dependency needs brace tracking, the scan
breaks on interpolated strings, and it would orphan `DeclaringTypeOf`. `RepositoryDependentsIn` reads the compiled IL with
Mono.Cecil (0.11.6, already brought in by ArchUnitNET, no new package), walks the compiler-made lambda and async types and
reports them against the writing type, so D4's "type dependencies" detection stands. Release check, run before building it:
Cecil sees `RatchetFixtures.AsyncDependent`'s `IRepository<Team>` edge in `<ResolveAsync>d__0` with `-c Release`.
